using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

// Cell reservation: a robot may only move towards a cell after claiming it in the occupancy buffer, and it
// releases the cell it leaves only once it has fully arrived. Two robots can therefore never share a cell.
// The job is scheduled once per priority level (carriers first) and runs single-threaded because every robot
// writes into the same occupancy buffer.
[BurstCompile]
[WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)]
public partial struct MovementJob : IJobEntity
{
    public BufferLookup<CellOccupant> OccupantLookup;
    [ReadOnly] public ComponentLookup<RobotIntent> IntentLookup;
    public BlobAssetReference<GridMapBlob> Map;
    public Entity Grid;
    public float DeltaTime;
    public float BatteryPerCell;
    public int Priority;

    public void Execute(Entity entity, ref RobotNavigation navigation, ref RobotPosition position, ref LocalTransform transform,
        ref RobotBattery battery, in RobotConfig config, in RobotDestination destination, in DynamicBuffer<PathNode> path,
        EnabledRefRW<PathRequest> request)
    {
        int priority = IntentLookup[entity].Priority;
        if (priority != Priority)
            return;

        var occupants = OccupantLookup[Grid];

        if (navigation.Moving)
        {
            if (!Move(ref navigation, ref position, ref transform, config, path, occupants))
                return;

            battery.Level = math.max(0f, battery.Level - BatteryPerCell);
        }

        if (request.ValueRO)
            return;

        if (navigation.PathIndex >= path.Length)
        {
            if (!position.Cell.Equals(destination.Cell))
                Wait(ref navigation, request, config, false);
            return;
        }

        int2 next = path[navigation.PathIndex].Cell;
        int nextIndex = Map.Value.Index(next);
        var occupant = occupants[nextIndex].Robot;

        if (occupant == Entity.Null || occupant == entity)
        {
            occupants[nextIndex] = new CellOccupant { Robot = entity };
            navigation.Moving = true;
            navigation.BlockedTime = 0f;
            return;
        }

        // Two robots that want each other's cell will wait forever: the one with lower priority gives way
        // immediately by planning around the other robot, instead of waiting for the repath delay.
        var other = IntentLookup[occupant];
        bool headOn = other.HasNext && other.Next.Equals(position.Cell);

        if (headOn && HasLowerPriority(entity, priority, occupant, other.Priority))
        {
            navigation.AvoidRobots = true;
            request.ValueRW = true;
            return;
        }

        Wait(ref navigation, request, config, true);
    }

    bool Move(ref RobotNavigation navigation, ref RobotPosition position, ref LocalTransform transform, in RobotConfig config,
        in DynamicBuffer<PathNode> path, DynamicBuffer<CellOccupant> occupants)
    {
        int2 next = path[navigation.PathIndex].Cell;
        float3 target = new float3(next.x, transform.Position.y, next.y);
        float3 delta = target - transform.Position;
        float distance = math.length(delta);
        float step = config.MetersPerSecond * DeltaTime;

        if (distance > 0.0001f)
        {
            var look = quaternion.LookRotationSafe(delta, math.up());
            transform.Rotation = math.slerp(transform.Rotation, look, math.saturate(DeltaTime * 12f));
        }

        if (step < distance)
        {
            transform.Position += delta / distance * step;
            return false;
        }

        transform.Position = target;
        occupants[Map.Value.Index(position.Cell)] = default;
        position.Cell = next;
        navigation.PathIndex++;
        navigation.Moving = false;
        return true;
    }

    void Wait(ref RobotNavigation navigation, EnabledRefRW<PathRequest> request, in RobotConfig config, bool avoidRobots)
    {
        navigation.BlockedTime += DeltaTime;

        if (navigation.BlockedTime < config.RepathDelay)
            return;

        navigation.AvoidRobots = avoidRobots;
        request.ValueRW = true;
    }

    static bool HasLowerPriority(Entity self, int selfPriority, Entity other, int otherPriority)
    {
        return selfPriority < otherPriority || (selfPriority == otherPriority && self.Index < other.Index);
    }
}
