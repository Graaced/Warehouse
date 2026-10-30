using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(WarehouseSimulationGroup))]
public partial struct WarehouseGenerationSystem : ISystem
{
    BlobAssetReference<GridMapBlob> map;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<ConfigWarehouse>();
    }

    public void OnDestroy(ref SystemState state)
    {
        if (map.IsCreated)
            map.Dispose();
    }

    public void OnUpdate(ref SystemState state)
    {
        state.Enabled = false;

        var config = SystemAPI.GetSingleton<ConfigWarehouse>();
        var layout = WarehouseLayout.Create(config);

        if (!layout.Fits)
        {
            Debug.LogError("[WarehouseGeneration] Not enough space for packing stations, chargers or loading bays!");
            return;
        }

        var em = state.EntityManager;
        int cellCount = layout.Width * layout.Height;
        var kinds = new NativeArray<CellKind>(cellCount, Allocator.Temp);
        var occupants = new NativeArray<CellOccupant>(cellCount, Allocator.Temp);

        var shelves = SpawnShelves(em, config, layout, kinds);
        SpawnPackingStations(em, config, layout, kinds);
        SpawnRightColumn(em, config, layout, kinds);
        SpawnParkingAndRobots(em, config, layout, kinds, occupants);
        SpawnFloor(em, config, layout);

        for (int i = 0; i < cellCount; i++)
        {
            if (kinds[i] == CellKind.Floor && layout.IsParkingArea(GridUtils.IndexToCell(i, layout.Width)))
                kinds[i] = CellKind.ParkingLane;
        }

        map = BuildMap(layout, kinds);

        var grid = em.CreateEntity();
        em.AddComponentData(grid, new GridMap { Value = map });
        em.AddBuffer<CellOccupant>(grid).AddRange(occupants);
        em.AddBuffer<GridShelf>(grid).AddRange(shelves.Reinterpret<GridShelf>());
        em.CreateEntity(typeof(WarehouseStats));

        kinds.Dispose();
        occupants.Dispose();
        shelves.Dispose();
    }

    static NativeArray<Entity> SpawnShelves(EntityManager em, in ConfigWarehouse config, in WarehouseLayout layout,
        NativeArray<CellKind> kinds)
    {
        var shelves = em.Instantiate(config.Shelf, layout.ShelfCount, Allocator.Temp);
        var template = em.GetComponentData<LocalTransform>(config.Shelf);

        for (int i = 0; i < shelves.Length; i++)
        {
            layout.Shelf(i, out var cell, out var access);
            Place(em, shelves[i], template, cell);
            em.SetComponentData(shelves[i], new ShelfCell { Cell = cell, Access = access });
            kinds[Index(layout, cell)] = CellKind.Shelf;
        }

        return shelves;
    }

    static void SpawnPackingStations(EntityManager em, in ConfigWarehouse config, in WarehouseLayout layout,
        NativeArray<CellKind> kinds)
    {
        for (int i = 0; i < config.PackingStationCount; i++)
            SpawnStation(em, config.PackingStation, StationKind.Packing, layout, layout.PackingStation(i), kinds);
    }

    static void SpawnRightColumn(EntityManager em, in ConfigWarehouse config, in WarehouseLayout layout,
        NativeArray<CellKind> kinds)
    {
        for (int i = 0; i < layout.RightColumnCount; i++)
        {
            var center = layout.RightColumn(i);

            if (layout.IsLoadingBay(i))
            {
                SpawnStation(em, config.LoadingBay, StationKind.Loading, layout, center, kinds);
                continue;
            }

            var charger = em.Instantiate(config.Charger);
            Place(em, charger, em.GetComponentData<LocalTransform>(config.Charger), center);
            em.SetComponentData(charger, new ChargerPosition { Cell = center });

            for (int dz = -layout.StationHalfLength; dz <= layout.StationHalfLength; dz++)
                kinds[Index(layout, center + new int2(0, dz))] = CellKind.Charger;
        }
    }

    static void SpawnStation(EntityManager em, Entity prefab, StationKind kind, in WarehouseLayout layout, int2 center,
        NativeArray<CellKind> kinds)
    {
        var station = em.Instantiate(prefab);
        Place(em, station, em.GetComponentData<LocalTransform>(prefab), center);
        em.SetComponentData(station, new StationLayout { Kind = kind, Center = center, Approach = layout.ApproachCell(center, kind) });

        var stationKind = kind == StationKind.Packing ? CellKind.PackingStation : CellKind.LoadingBay;

        // Service slots come first in the buffer and queue slots after them: the robot logic relies on the
        // Service flag only, but keeping a stable order makes the buffer easy to inspect.
        var slots = em.GetBuffer<StationSlot>(station);

        for (int dz = -layout.StationHalfLength; dz <= layout.StationHalfLength; dz++)
        {
            var service = layout.ServiceCell(center, kind, dz);
            kinds[Index(layout, center + new int2(0, dz))] = stationKind;
            kinds[Index(layout, service)] = CellKind.StationService;
            slots.Add(new StationSlot { Cell = service, Service = true });
        }

        for (int dz = -layout.StationHalfLength; dz <= layout.StationHalfLength; dz++)
            slots.Add(new StationSlot { Cell = layout.QueueCell(center, kind, dz) });
    }

    static void SpawnParkingAndRobots(EntityManager em, in ConfigWarehouse config, in WarehouseLayout layout,
        NativeArray<CellKind> kinds, NativeArray<CellOccupant> occupants)
    {
        var slots = em.Instantiate(config.ParkingSlot, config.MaxRobots, Allocator.Temp);
        var robots = em.Instantiate(config.Robot, config.InitialRobots, Allocator.Temp);
        var slotTemplate = em.GetComponentData<LocalTransform>(config.ParkingSlot);
        var robotTemplate = em.GetComponentData<LocalTransform>(config.Robot);

        for (int i = 0; i < slots.Length; i++)
        {
            var cell = layout.ParkingCell(i);
            bool active = i < robots.Length;

            Place(em, slots[i], slotTemplate, cell);
            em.SetComponentData(slots[i], new ParkingSlot { Cell = cell, Index = i, Active = active });
            kinds[Index(layout, cell)] = CellKind.Parking;

            if (!active)
            {
                em.AddComponent<DisableRendering>(slots[i]);
                continue;
            }

            Place(em, robots[i], robotTemplate, cell);
            em.SetComponentData(robots[i], new RobotPosition { Cell = cell });
            em.SetComponentData(robots[i], new RobotDestination { Cell = cell });
            em.SetComponentData(robots[i], new RobotAssignment { ParkingSlot = slots[i] });
            occupants[Index(layout, cell)] = new CellOccupant { Robot = robots[i] };
        }

        slots.Dispose();
        robots.Dispose();
    }

    static void SpawnFloor(EntityManager em, in ConfigWarehouse config, in WarehouseLayout layout)
    {
        var tiles = em.Instantiate(config.FloorTile, layout.Width * layout.Height, Allocator.Temp);
        var template = em.GetComponentData<LocalTransform>(config.FloorTile);

        for (int i = 0; i < tiles.Length; i++)
            Place(em, tiles[i], template, GridUtils.IndexToCell(i, layout.Width));

        tiles.Dispose();
    }

    static BlobAssetReference<GridMapBlob> BuildMap(in WarehouseLayout layout, NativeArray<CellKind> kinds)
    {
        var builder = new BlobBuilder(Allocator.Temp);
        ref var root = ref builder.ConstructRoot<GridMapBlob>();
        root.Width = layout.Width;
        root.Height = layout.Height;

        var blobKinds = builder.Allocate(ref root.Kinds, kinds.Length);
        var blobLanes = builder.Allocate(ref root.Lanes, kinds.Length);

        for (int i = 0; i < kinds.Length; i++)
        {
            blobKinds[i] = kinds[i];
            blobLanes[i] = kinds[i] == CellKind.Floor ? layout.Lane(GridUtils.IndexToCell(i, layout.Width)) : CellLane.None;
        }

        var result = builder.CreateBlobAssetReference<GridMapBlob>(Allocator.Persistent);
        builder.Dispose();
        return result;
    }

    // Prefabs carry their own height and scale in their LocalTransform, so only X and Z come from the grid.
    static void Place(EntityManager em, Entity entity, LocalTransform template, int2 cell)
    {
        template.Position = new float3(cell.x, template.Position.y, cell.y);
        em.SetComponentData(entity, template);
    }

    static int Index(in WarehouseLayout layout, int2 cell)
    {
        return cell.y * layout.Width + cell.x;
    }
}
