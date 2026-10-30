using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

// Runs single-threaded on purpose: robots compete for the same parking and station slots, and processing them
// one after the other keeps every reservation consistent without locks.
[BurstCompile]
[WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)]
public partial struct RobotStateMachineJob : IJobEntity
{
    public ReservationTable Table;

    [ReadOnly] public NativeArray<Entity> Stations;
    [ReadOnly] public NativeArray<StationLayout> StationLayouts;
    [ReadOnly] public NativeArray<Entity> ParkingEntities;
    [ReadOnly] public NativeArray<ParkingSlot> ParkingSlots;

    [ReadOnly] public BufferLookup<StationSlot> SlotLookup;
    [ReadOnly] public BufferLookup<CellOccupant> OccupantLookup;
    [ReadOnly] public ComponentLookup<OrderShelf> OrderShelfLookup;
    public ComponentLookup<ShelfStock> StockLookup;
    public EntityCommandBuffer Commands;

    public BlobAssetReference<GridMapBlob> Map;
    public Entity Grid;
    public ConfigWarehouse Config;
    public double Now;
    public Random Random;

    public void Execute(Entity entity, ref RobotState robotState, ref ActionTimer timer, ref CurrentOrder currentOrder,
        ref RobotAssignment assignment, ref RobotDestination destination, in RobotPosition position,
        in RobotNavigation navigation, EnabledRefRW<PathRequest> request)
    {
        bool arrived = !navigation.Moving && !request.ValueRO && position.Cell.Equals(destination.Cell);

        switch (robotState.Value)
        {
            case RobotStateKind.Idle:
                if (assignment.ParkingSlot == Entity.Null)
                    ReserveParking(ref assignment, ref destination, request, position.Cell);
                break;

            case RobotStateKind.ToShelf:
                if (arrived)
                {
                    robotState.Value = RobotStateKind.PickingUp;
                    timer.Expiry = Now + Config.LiftSeconds;
                }
                break;

            case RobotStateKind.PickingUp:
                if (Now >= timer.Expiry && TryPickUp(currentOrder.Order))
                    robotState.Value = RobotStateKind.Transporting;
                break;

            case RobotStateKind.Transporting:
                if (UpdateTransport(entity, ref assignment, ref destination, request, position.Cell, arrived))
                {
                    robotState.Value = RobotStateKind.Unloading;
                    timer.Expiry = Now + Config.PackagingSeconds;
                }
                break;

            case RobotStateKind.Unloading:
                if (Now < timer.Expiry)
                    break;

                ReleaseStation(ref assignment);
                Commands.DestroyEntity(currentOrder.Order);
                currentOrder.Order = Entity.Null;
                robotState.Value = RobotStateKind.Idle;
                break;
        }
    }

    bool TryPickUp(Entity order)
    {
        var shelf = OrderShelfLookup[order].Shelf;
        var stock = StockLookup[shelf];

        if (stock.Empty)
            return false;

        stock.Empty = true;
        stock.RespawnAt = Now + Random.NextFloat(Config.RespawnMinSeconds, Config.RespawnMaxSeconds);
        StockLookup[shelf] = stock;
        return true;
    }

    // A carrier never reserves a station slot from far away: holding slots while still crossing the warehouse
    // starved the queues and left counters idle. It heads to the station approach cell first and only claims
    // a slot once it is within ApproachDistance.
    bool UpdateTransport(Entity robot, ref RobotAssignment assignment, ref RobotDestination destination,
        EnabledRefRW<PathRequest> request, int2 cell, bool arrived)
    {
        if (assignment.Stage == StationStage.None)
        {
            ChooseStation(ref assignment, ref destination, request, cell);
            return false;
        }

        if (!Table.StationIndex.TryGetValue(assignment.Station, out int station))
        {
            assignment = new RobotAssignment { ParkingSlot = assignment.ParkingSlot };
            return false;
        }

        switch (assignment.Stage)
        {
            case StationStage.Approaching:
                if (GridUtils.Distance(cell, StationLayouts[station].Center) <= Config.ApproachDistance)
                    TryTakeSlot(robot, station, ref assignment, ref destination, request, cell);
                return false;

            case StationStage.ToQueue:
                if (arrived)
                {
                    assignment.Stage = StationStage.InQueue;
                    assignment.QueuedAt = Now;
                    Table.MarkWaiting(station, assignment.Slot, Now);
                }
                return false;

            case StationStage.InQueue:
                TryPromote(robot, station, ref assignment, ref destination, request);
                return false;

            default:
                return arrived;
        }
    }

    // Stations are weighted by distance plus the robots already heading there, so carriers spread over the
    // counters instead of all choosing the nearest one.
    void ChooseStation(ref RobotAssignment assignment, ref RobotDestination destination, EnabledRefRW<PathRequest> request, int2 cell)
    {
        int best = -1;
        int bestScore = int.MaxValue;

        for (int i = 0; i < Stations.Length; i++)
        {
            int score = GridUtils.Distance(StationLayouts[i].Center, cell) + Table.StationLoad[i] * Config.StationLoadWeight;
            if (score < bestScore)
            {
                bestScore = score;
                best = i;
            }
        }

        if (best < 0)
            return;

        Table.StationLoad[best]++;
        assignment.Station = Stations[best];
        assignment.Stage = StationStage.Approaching;
        GoTo(ref destination, request, StationLayouts[best].Approach);
    }

    // A newcomer may go straight to a free counter slot only if nobody is already waiting in the queue,
    // otherwise it joins the queue so that robots are served in arrival order.
    void TryTakeSlot(Entity robot, int station, ref RobotAssignment assignment, ref RobotDestination destination,
        EnabledRefRW<PathRequest> request, int2 cell)
    {
        var slots = SlotLookup[Stations[station]];
        bool queueWaiting = false;
        int service = -1;
        int queue = -1;
        int queueDistance = int.MaxValue;

        for (int i = 0; i < slots.Length; i++)
        {
            int key = Table.SlotKey(station, i);

            if (!slots[i].Service && Table.SlotWaiting[key])
                queueWaiting = true;

            if (Table.SlotRobot[key] != Entity.Null)
                continue;

            if (slots[i].Service)
            {
                if (service < 0 && !IsOccupied(slots[i].Cell))
                    service = i;
            }
            else if (GridUtils.Distance(slots[i].Cell, cell) < queueDistance)
            {
                queueDistance = GridUtils.Distance(slots[i].Cell, cell);
                queue = i;
            }
        }

        int target = service >= 0 && !queueWaiting ? service : queue;
        if (target >= 0)
            TakeSlot(robot, station, target, slots[target], ref assignment, ref destination, request);
    }

    // Only the robot that has been waiting the longest may leave the queue, and only towards a counter slot that
    // is physically empty: the previous robot may still be standing there while it drives away.
    void TryPromote(Entity robot, int station, ref RobotAssignment assignment, ref RobotDestination destination,
        EnabledRefRW<PathRequest> request)
    {
        var slots = SlotLookup[Stations[station]];
        int target = -1;

        for (int i = 0; i < slots.Length; i++)
        {
            int key = Table.SlotKey(station, i);

            if (!slots[i].Service && Table.SlotWaiting[key] && Table.SlotQueuedAt[key] < assignment.QueuedAt)
                return;

            if (slots[i].Service && target < 0 && Table.SlotRobot[key] == Entity.Null && !IsOccupied(slots[i].Cell))
                target = i;
        }

        if (target < 0)
            return;

        Table.Release(station, assignment.Slot);
        TakeSlot(robot, station, target, slots[target], ref assignment, ref destination, request);
    }

    void TakeSlot(Entity robot, int station, int index, StationSlot slot, ref RobotAssignment assignment,
        ref RobotDestination destination, EnabledRefRW<PathRequest> request)
    {
        Table.Occupy(station, index, robot);
        assignment.Slot = index;
        assignment.Stage = slot.Service ? StationStage.Serving : StationStage.ToQueue;
        assignment.QueuedAt = 0;
        GoTo(ref destination, request, slot.Cell);
    }

    void ReleaseStation(ref RobotAssignment assignment)
    {
        if (Table.StationIndex.TryGetValue(assignment.Station, out int station))
        {
            Table.Release(station, assignment.Slot);
            Table.StationLoad[station]--;
        }

        assignment = new RobotAssignment { ParkingSlot = assignment.ParkingSlot };
    }

    void ReserveParking(ref RobotAssignment assignment, ref RobotDestination destination, EnabledRefRW<PathRequest> request, int2 cell)
    {
        int best = -1;
        int bestDistance = int.MaxValue;

        for (int i = 0; i < ParkingSlots.Length; i++)
        {
            if (!ParkingSlots[i].Active || Table.ParkingTaken[i])
                continue;

            int distance = GridUtils.Distance(ParkingSlots[i].Cell, cell);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = i;
            }
        }

        if (best < 0)
            return;

        Table.ParkingTaken[best] = true;
        assignment.ParkingSlot = ParkingEntities[best];
        GoTo(ref destination, request, ParkingSlots[best].Cell);
    }

    bool IsOccupied(int2 cell)
    {
        return OccupantLookup[Grid][Map.Value.Index(cell)].Robot != Entity.Null;
    }

    static void GoTo(ref RobotDestination destination, EnabledRefRW<PathRequest> request, int2 cell)
    {
        destination.Cell = cell;
        request.ValueRW = true;
    }
}
