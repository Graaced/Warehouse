using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

public partial struct WHGenerationSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<ConfigWarehouse>();
    }

    public void OnUpdate(ref SystemState state)
    {
        state.Enabled = false;

        var config = SystemAPI.GetSingleton<ConfigWarehouse>();
        var em = state.EntityManager;

        int span = config.ShelfColumnPairs * 2 + (config.ShelfColumnPairs - 1) * config.AisleWidth;
        int slotRows = (config.MaxRobots + span - 1) / span;
        int parkingHeight = slotRows + slotRows / 2;

        int width = span + config.SideAisleWidth * 2 + 4;
        int height = parkingHeight + config.BottomBlockRows + config.TopBlockRows + config.AisleWidth * 3 + 1;

        int shelvesX = config.SideAisleWidth + 2;
        int bottomBlockZ = parkingHeight + config.AisleWidth + 1;
        int topBlockZ = bottomBlockZ + config.BottomBlockRows + config.AisleWidth;
        int rightColumnX = width - 2;
        int rightCount = config.LoadingBayCount + config.ChargerCount;
        int stationHalfLength = config.StationLength / 2;

        if (config.PackingStationCount * (config.StationLength + 1) > height - 2 ||
            rightCount * (config.StationLength + 1) > height - 2)
        {
            Debug.LogError("[WHGeneration] Not enough space for packing stations, chargers or loading bays!");
            return;
        }

        var cells = new NativeArray<CellState>(width * height, Allocator.Temp);
        var occupants = new NativeArray<CellOccupant>(width * height, Allocator.Temp);

        for (int pair = 0; pair < config.ShelfColumnPairs; pair++)
        {
            for (int column = 0; column < 2; column++)
            {
                int x = shelvesX + pair * (2 + config.AisleWidth) + column;

                for (int row = 0; row < config.BottomBlockRows; row++)
                    PlaceShelf(em, cells, width, config.Shelf, new int2(x, bottomBlockZ + row));

                for (int row = 0; row < config.TopBlockRows; row++)
                    PlaceShelf(em, cells, width, config.Shelf, new int2(x, topBlockZ + row));
            }
        }

        for (int i = 0; i < config.PackingStationCount; i++)
        {
            var cell = new int2(1, Distribute(i, config.PackingStationCount, 1, height - 2));
            var station = Spawn(em, config.PackingStation, cell);
            em.SetComponentData(station, new StationPosition { Cell = cell });

            var slots = em.GetBuffer<StationSlot>(station);

            for (int dz = -stationHalfLength; dz <= stationHalfLength; dz++)
            {
                Mark(cells, width, cell + new int2(0, dz), CellKind.PackingStation);
                Mark(cells, width, cell + new int2(1, dz), CellKind.PackingService);
                slots.Add(new StationSlot { Cell = cell + new int2(1, dz), Service = true });
            }

            for (int dz = -stationHalfLength; dz <= stationHalfLength; dz++)
                slots.Add(new StationSlot { Cell = cell + new int2(4, dz) });
        }

        for (int i = 0; i < rightCount; i++)
        {
            var cell = new int2(rightColumnX, Distribute(i, rightCount, 1, height - 2));
            bool isBay = i < config.LoadingBayCount;

            if (isBay)
            {
                var bay = Spawn(em, config.LoadingBay, cell);
                em.SetComponentData(bay, new LoadingAreaPosition { Cell = cell });
            }
            else
            {
                var charger = Spawn(em, config.Charger, cell);
                em.SetComponentData(charger, new ChargerPosition { Cell = cell });
            }

            for (int dz = -stationHalfLength; dz <= stationHalfLength; dz++)
                Mark(cells, width, cell + new int2(0, dz), isBay ? CellKind.LoadingBay : CellKind.Charger);
        }

        for (int i = 0; i < config.MaxRobots; i++)
        {
            int row = i / span;
            var cell = new int2(shelvesX + i % span, parkingHeight - row - (row + 1) / 2);
            bool active = i < config.InitialRobots;

            var slot = Place(em, cells, width, config.ParkingSlot, cell, CellKind.Parking);

            if (!active)
            {
                em.SetComponentData(slot, new ParkingSlot { Cell = cell, Index = i });
                em.AddComponent<DisableRendering>(slot);
                continue;
            }

            var robot = Spawn(em, config.Robot, cell);
            em.SetComponentData(robot, new RobotPosition { Cell = cell });
            em.SetComponentData(robot, new RobotDestination { Cell = cell });
            em.SetComponentData(robot, new RobotAssignment { ParkingSlot = slot });
            em.SetComponentData(slot, new ParkingSlot { Cell = cell, Index = i, Active = true, Robot = robot });
            occupants[GridUtils.CellIndex(cell, width)] = new CellOccupant { Robot = robot };
        }

        for (int z = 1; z <= parkingHeight; z++)
        {
            for (int x = shelvesX; x < shelvesX + span; x++)
            {
                if (cells[GridUtils.CellIndex(new int2(x, z), width)].Kind == CellKind.Floor)
                    Mark(cells, width, new int2(x, z), CellKind.ParkingLane);
            }
        }

        int centerLaneZ = bottomBlockZ + config.BottomBlockRows;
        int centerSplitZ = centerLaneZ + config.AisleWidth / 2;
        int topLaneZ = topBlockZ + config.TopBlockRows;

        MarkLane(cells, width, shelvesX, span, parkingHeight + 1, bottomBlockZ, CellLane.Bottom);
        MarkLane(cells, width, shelvesX, span, centerLaneZ, centerSplitZ, CellLane.CenterEast);
        MarkLane(cells, width, shelvesX, span, centerSplitZ, topBlockZ, CellLane.CenterWest);
        MarkLane(cells, width, shelvesX, span, topLaneZ, height, CellLane.Top);

        for (int z = 0; z < height; z++)
            for (int x = 0; x < width; x++)
                Spawn(em, config.FloorTile, new int2(x, z));

        var grid = em.CreateEntity();
        em.AddComponent<TagGrid>(grid);
        em.AddComponentData(grid, new GridDimensions { Width = width, Height = height });
        em.AddBuffer<CellState>(grid).AddRange(cells);
        em.AddBuffer<CellOccupant>(grid).AddRange(occupants);

        cells.Dispose();
        occupants.Dispose();
    }

    static int Distribute(int index, int count, int from, int length)
    {
        return from + (index * 2 + 1) * length / (count * 2);
    }

    static void PlaceShelf(EntityManager em, NativeArray<CellState> cells, int width, Entity prefab, int2 cell)
    {
        var shelf = Place(em, cells, width, prefab, cell, CellKind.Shelf);
        em.SetComponentData(shelf, new ShelfCell { Cell = cell });
    }

    static Entity Place(EntityManager em, NativeArray<CellState> cells, int width, Entity prefab, int2 cell, CellKind kind)
    {
        Mark(cells, width, cell, kind);
        return Spawn(em, prefab, cell);
    }

    static void MarkLane(NativeArray<CellState> cells, int width, int fromX, int span, int fromZ, int toZ, CellLane lane)
    {
        for (int z = fromZ; z < toZ; z++)
        {
            for (int x = fromX; x < fromX + span; x++)
            {
                int index = GridUtils.CellIndex(new int2(x, z), width);
                var cell = cells[index];

                if (cell.Kind != CellKind.Floor)
                    continue;

                cell.Lane = lane;
                cells[index] = cell;
            }
        }
    }

    static void Mark(NativeArray<CellState> cells, int width, int2 cell, CellKind kind)
    {
        cells[GridUtils.CellIndex(cell, width)] = new CellState { Kind = kind, Fill = CellFill.Full };
    }

    static Entity Spawn(EntityManager em, Entity prefab, int2 cell)
    {
        var entity = em.Instantiate(prefab);
        var transform = em.GetComponentData<LocalTransform>(prefab);
        transform.Position = new float3(cell.x, transform.Position.y, cell.y);
        em.SetComponentData(entity, transform);
        return entity;
    }
}
