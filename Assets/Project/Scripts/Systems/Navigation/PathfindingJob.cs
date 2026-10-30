using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[BurstCompile]
public partial struct PathfindingJob : IJobEntity
{
    public BlobAssetReference<GridMapBlob> Map;
    [ReadOnly] public BufferLookup<CellOccupant> OccupantLookup;
    public Entity Grid;
    public int LanePenalty;
    public int Seed;

    public void Execute(Entity entity, ref RobotNavigation navigation, in RobotPosition position, in RobotDestination destination,
        in RobotState robotState, DynamicBuffer<PathNode> path, EnabledRefRW<PathRequest> request)
    {
        if (!request.ValueRO || navigation.Moving)
            return;

        ref var map = ref Map.Value;
        var occupants = OccupantLookup[Grid].AsNativeArray();

        var query = new PathQuery
        {
            Robot = entity,
            Start = position.Cell,
            Goal = destination.Cell,
            Carrying = robotState.IsCarrying,
            AvoidRobots = navigation.AvoidRobots,
            FromService = map.Kind(position.Cell) == CellKind.PackingService
        };

        request.ValueRW = false;
        navigation.PathIndex = 0;
        navigation.BlockedTime = 0f;
        navigation.AvoidRobots = false;
        path.Clear();

        // A robot leaving a counter first plans around the neighbouring slots that are still occupied, so the
        // outer robots exit on their own free side. The middle robot, boxed in on both sides, gets a second plan
        // without that restriction and simply waits for one of its neighbours to leave.
        query.BlockOccupiedService = query.FromService;
        bool found = FindPath(ref map, occupants, query, path);

        if (!found && query.FromService)
        {
            query.BlockOccupiedService = false;
            found = FindPath(ref map, occupants, query, path);
        }

        if (!found && query.AvoidRobots)
            Sidestep(ref map, occupants, query, path);
    }

    bool FindPath(ref GridMapBlob map, NativeArray<CellOccupant> occupants, in PathQuery query, DynamicBuffer<PathNode> path)
    {
        if (query.Start.Equals(query.Goal))
            return true;

        int count = map.Width * map.Height;
        int start = map.Index(query.Start);
        int goal = map.Index(query.Goal);

        var cost = new NativeArray<int>(count, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
        var parent = new NativeArray<int>(count, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
        var closed = new NativeArray<bool>(count, Allocator.Temp);
        var open = new PathHeap(64, Allocator.Temp);

        for (int i = 0; i < count; i++)
            cost[i] = int.MaxValue;

        cost[start] = 0;
        open.Push(start, GridUtils.Distance(query.Start, query.Goal));
        bool found = false;

        while (!open.IsEmpty)
        {
            int current = open.Pop();

            if (current == goal)
            {
                found = true;
                break;
            }

            if (closed[current])
                continue;

            closed[current] = true;
            int2 cell = GridUtils.IndexToCell(current, map.Width);

            for (int d = 0; d < 4; d++)
            {
                int2 next = cell + GridUtils.Direction(d);
                if (!TrafficRules.CanEnter(ref map, occupants, query, cell, next))
                    continue;

                int nextIndex = map.Index(next);
                int nextCost = cost[current] + TrafficRules.StepCost(ref map, query, cell, next, LanePenalty);
                if (closed[nextIndex] || nextCost >= cost[nextIndex])
                    continue;

                cost[nextIndex] = nextCost;
                parent[nextIndex] = current;
                open.Push(nextIndex, nextCost + GridUtils.Distance(next, query.Goal));
            }
        }

        if (found)
            WritePath(ref map, parent, start, goal, path);

        cost.Dispose();
        parent.Dispose();
        closed.Dispose();
        open.Dispose();
        return found;
    }

    static void WritePath(ref GridMapBlob map, NativeArray<int> parent, int start, int goal, DynamicBuffer<PathNode> path)
    {
        for (int index = goal; index != start; index = parent[index])
            path.Add(new PathNode { Cell = GridUtils.IndexToCell(index, map.Width) });

        for (int i = 0, j = path.Length - 1; i < j; i++, j--)
        {
            var swap = path[i];
            path[i] = path[j];
            path[j] = swap;
        }
    }

    // Last resort when even a robot-avoiding plan fails: step into any free neighbour cell to break the jam,
    // then plan again from there once the repath delay expires.
    void Sidestep(ref GridMapBlob map, NativeArray<CellOccupant> occupants, PathQuery query, DynamicBuffer<PathNode> path)
    {
        query.Goal = new int2(-1);
        int offset = query.Robot.Index + Seed;

        for (int i = 0; i < 4; i++)
        {
            int2 next = query.Start + GridUtils.Direction((i + offset) & 3);
            if (TrafficRules.CanEnter(ref map, occupants, query, query.Start, next))
            {
                path.Add(new PathNode { Cell = next });
                return;
            }
        }
    }
}
