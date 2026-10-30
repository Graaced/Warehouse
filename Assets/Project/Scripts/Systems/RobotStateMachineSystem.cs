using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(PathfindingSystem))]
[BurstCompile]
public partial struct RobotStateMachineSystem : ISystem
{
    const int ApproachDistance = 6;
    const int LoadWeight = 4;

    Random random;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<ConfigWarehouse>();
        state.RequireForUpdate<TagGrid>();
        state.RequireForUpdate<TagRobot>();
        random = Random.CreateFromIndex(11);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        state.CompleteDependency();

        var config = SystemAPI.GetSingleton<ConfigWarehouse>();
        var grid = SystemAPI.GetSingletonEntity<TagGrid>();
        int width = SystemAPI.GetComponent<GridDimensions>(grid).Width;
        var cells = SystemAPI.GetBuffer<CellState>(grid);
        var occupants = SystemAPI.GetBuffer<CellOccupant>(grid);
        double now = SystemAPI.Time.ElapsedTime;

        var freeSlots = new NativeList<Entity>(Allocator.Temp);
        foreach (var (slot, entity) in SystemAPI.Query<RefRO<ParkingSlot>>().WithEntityAccess())
        {
            if (slot.ValueRO.Active && slot.ValueRO.Robot == Entity.Null)
                freeSlots.Add(entity);
        }

        var stations = SystemAPI.QueryBuilder().WithAll<TagPackingStation>().Build().ToEntityArray(Allocator.Temp);
        var loads = new NativeHashMap<Entity, int>(stations.Length, Allocator.Temp);

        foreach (var station in stations)
            loads[station] = 0;

        foreach (var assignment in SystemAPI.Query<RefRO<RobotAssignment>>())
        {
            if (assignment.ValueRO.Station != Entity.Null)
                loads[assignment.ValueRO.Station] += 1;
        }

        var ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (robotState, timer, currentOrder, entity) in SystemAPI
                     .Query<RefRW<RobotState>, RefRW<ActionTimer>, RefRW<CurrentOrder>>()
                     .WithEntityAccess())
        {
            switch (robotState.ValueRO.Value)
            {
                case RobotStateKind.Idle:
                    if (SystemAPI.GetComponent<RobotAssignment>(entity).ParkingSlot == Entity.Null)
                        ReserveParking(ref state, entity, freeSlots);
                    break;

                case RobotStateKind.ToShelf:
                    if (HasArrived(ref state, entity))
                    {
                        robotState.ValueRW.Value = RobotStateKind.PickingUp;
                        timer.ValueRW.Expiry = now + config.LiftSeconds;
                    }
                    break;

                case RobotStateKind.PickingUp:
                    if (now >= timer.ValueRO.Expiry && TryPickUp(ref state, currentOrder.ValueRO.Order, cells, width, now, config))
                        robotState.ValueRW.Value = RobotStateKind.Transporting;
                    break;

                case RobotStateKind.Transporting:
                    if (UpdateTransporting(ref state, entity, stations, loads, occupants, width, now))
                    {
                        robotState.ValueRW.Value = RobotStateKind.Unloading;
                        timer.ValueRW.Expiry = now + config.PackagingSeconds;
                    }
                    break;

                case RobotStateKind.Unloading:
                    if (now < timer.ValueRO.Expiry)
                        break;

                    ReleaseStation(ref state, entity);
                    ecb.DestroyEntity(currentOrder.ValueRO.Order);
                    currentOrder.ValueRW.Order = Entity.Null;
                    robotState.ValueRW.Value = RobotStateKind.Idle;
                    break;
            }
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
        freeSlots.Dispose();
        stations.Dispose();
        loads.Dispose();
    }

    bool TryPickUp(ref SystemState state, Entity order, DynamicBuffer<CellState> cells, int width, double now, ConfigWarehouse config)
    {
        int index = GridUtils.CellIndex(SystemAPI.GetComponent<DestinationCell>(order).Cell, width);
        var cell = cells[index];

        if (cell.Fill != CellFill.Full)
            return false;

        cell.Fill = CellFill.Transparent;
        cell.RespawnTimer = now + random.NextFloat(config.RespawnMinSeconds, config.RespawnMaxSeconds);
        cells[index] = cell;
        return true;
    }

    bool UpdateTransporting(ref SystemState state, Entity robot, NativeArray<Entity> stations, NativeHashMap<Entity, int> loads,
        DynamicBuffer<CellOccupant> occupants, int width, double now)
    {
        var assignment = SystemAPI.GetComponent<RobotAssignment>(robot);

        if (assignment.Station == Entity.Null)
        {
            ChooseStation(ref state, robot, stations, loads);
            return false;
        }

        var slots = SystemAPI.GetBuffer<StationSlot>(assignment.Station);

        if (assignment.StationSlot < 0)
        {
            int2 position = SystemAPI.GetComponent<RobotPosition>(robot).Cell;
            int2 stationCell = SystemAPI.GetComponent<StationPosition>(assignment.Station).Cell;

            if (GridUtils.Distance(position, stationCell) <= ApproachDistance)
                TryTakeSlot(ref state, robot, assignment.Station, slots, occupants, width);

            return false;
        }

        var slot = slots[assignment.StationSlot];
        bool arrived = HasArrived(ref state, robot);

        if (slot.Service)
            return arrived;

        if (arrived && !slot.Arrived)
        {
            slot.Arrived = true;
            slot.ArrivedAt = now;
            slots[assignment.StationSlot] = slot;
        }

        if (slot.Arrived)
            TryPromote(ref state, robot, assignment, slots, occupants, width);

        return false;
    }

    void ChooseStation(ref SystemState state, Entity robot, NativeArray<Entity> stations, NativeHashMap<Entity, int> loads)
    {
        int2 position = SystemAPI.GetComponent<RobotPosition>(robot).Cell;
        var best = Entity.Null;
        int bestScore = int.MaxValue;

        foreach (var station in stations)
        {
            int score = GridUtils.Distance(SystemAPI.GetComponent<StationPosition>(station).Cell, position) + loads[station] * LoadWeight;
            if (score < bestScore)
            {
                bestScore = score;
                best = station;
            }
        }

        if (best == Entity.Null)
            return;

        loads[best] += 1;

        var assignment = SystemAPI.GetComponent<RobotAssignment>(robot);
        assignment.Station = best;
        assignment.StationSlot = -1;
        SystemAPI.SetComponent(robot, assignment);

        GoTo(ref state, robot, SystemAPI.GetComponent<StationPosition>(best).Cell + new int2(ApproachDistance - 1, 0));
    }

    void TryTakeSlot(ref SystemState state, Entity robot, Entity station, DynamicBuffer<StationSlot> slots,
        DynamicBuffer<CellOccupant> occupants, int width)
    {
        int2 position = SystemAPI.GetComponent<RobotPosition>(robot).Cell;
        int service = -1;
        int queue = -1;
        int queueDistance = int.MaxValue;
        bool queueWaiting = false;

        for (int i = 0; i < slots.Length; i++)
        {
            var slot = slots[i];

            if (!slot.Service && slot.Arrived)
                queueWaiting = true;

            if (slot.Robot != Entity.Null)
                continue;

            if (slot.Service && service < 0 && occupants[GridUtils.CellIndex(slot.Cell, width)].Robot == Entity.Null)
                service = i;

            if (!slot.Service && GridUtils.Distance(slot.Cell, position) < queueDistance)
            {
                queueDistance = GridUtils.Distance(slot.Cell, position);
                queue = i;
            }
        }

        int target = service >= 0 && !queueWaiting ? service : queue;

        if (target >= 0)
            MoveToStationSlot(ref state, robot, station, target);
    }

    void TryPromote(ref SystemState state, Entity robot, RobotAssignment assignment, DynamicBuffer<StationSlot> slots,
        DynamicBuffer<CellOccupant> occupants, int width)
    {
        int current = assignment.StationSlot;
        int target = -1;

        for (int i = 0; i < slots.Length; i++)
        {
            var slot = slots[i];

            if (!slot.Service && slot.Arrived && slot.ArrivedAt < slots[current].ArrivedAt)
                return;

            bool free = slot.Service && slot.Robot == Entity.Null &&
                        occupants[GridUtils.CellIndex(slot.Cell, width)].Robot == Entity.Null;

            if (free && target < 0)
                target = i;
        }

        if (target < 0)
            return;

        slots[current] = new StationSlot { Cell = slots[current].Cell };
        MoveToStationSlot(ref state, robot, assignment.Station, target);
    }

    void MoveToStationSlot(ref SystemState state, Entity robot, Entity station, int slotIndex)
    {
        var slots = SystemAPI.GetBuffer<StationSlot>(station);
        var slot = slots[slotIndex];
        slot.Robot = robot;
        slots[slotIndex] = slot;

        var assignment = SystemAPI.GetComponent<RobotAssignment>(robot);
        assignment.Station = station;
        assignment.StationSlot = slotIndex;
        SystemAPI.SetComponent(robot, assignment);

        GoTo(ref state, robot, slot.Cell);
    }

    void ReleaseStation(ref SystemState state, Entity robot)
    {
        var assignment = SystemAPI.GetComponent<RobotAssignment>(robot);
        var slots = SystemAPI.GetBuffer<StationSlot>(assignment.Station);
        var slot = slots[assignment.StationSlot];
        slots[assignment.StationSlot] = new StationSlot { Cell = slot.Cell, Service = slot.Service };

        assignment.Station = Entity.Null;
        assignment.StationSlot = 0;
        SystemAPI.SetComponent(robot, assignment);
    }

    bool HasArrived(ref SystemState state, Entity robot)
    {
        var navigation = SystemAPI.GetComponent<RobotNavigation>(robot);
        var position = SystemAPI.GetComponent<RobotPosition>(robot);
        var destination = SystemAPI.GetComponent<RobotDestination>(robot);

        return !navigation.Moving && !SystemAPI.IsComponentEnabled<PathRequest>(robot) && position.Cell.Equals(destination.Cell);
    }

    void GoTo(ref SystemState state, Entity robot, int2 cell)
    {
        SystemAPI.SetComponent(robot, new RobotDestination { Cell = cell });
        SystemAPI.SetComponentEnabled<PathRequest>(robot, true);
    }

    void ReserveParking(ref SystemState state, Entity robot, NativeList<Entity> freeSlots)
    {
        if (freeSlots.Length == 0)
            return;

        int2 position = SystemAPI.GetComponent<RobotPosition>(robot).Cell;
        int best = 0;
        int bestDistance = int.MaxValue;

        for (int i = 0; i < freeSlots.Length; i++)
        {
            int distance = GridUtils.Distance(SystemAPI.GetComponent<ParkingSlot>(freeSlots[i]).Cell, position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = i;
            }
        }

        var slotEntity = freeSlots[best];
        freeSlots.RemoveAtSwapBack(best);

        var slot = SystemAPI.GetComponent<ParkingSlot>(slotEntity);
        slot.Robot = robot;
        SystemAPI.SetComponent(slotEntity, slot);

        var assignment = SystemAPI.GetComponent<RobotAssignment>(robot);
        assignment.ParkingSlot = slotEntity;
        SystemAPI.SetComponent(robot, assignment);

        GoTo(ref state, robot, slot.Cell);
    }
}
