using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[BurstCompile]
[WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)]
public partial struct PathfindingJob : IJobEntity
{
    [ReadOnly] public BufferLookup<CellState> CellLookup;
    [ReadOnly] public BufferLookup<CellOccupant> OccupantLookup;
    public Entity Grid;
    public int Width;
    public int Height;
    public int Seed;

    public void Execute(Entity entity, ref RobotNavigation navigation, in RobotPosition position, in RobotDestination destination,
        DynamicBuffer<PathNode> path, EnabledRefRW<PathRequest> request)
    {
        if (!request.ValueRO || navigation.Moving)
            return;

        var cells = CellLookup[Grid].AsNativeArray();
        var occupants = OccupantLookup[Grid].AsNativeArray();
        bool avoid = navigation.AvoidRobots;

        request.ValueRW = false;
        navigation.PathIndex = 0;
        navigation.BlockedTime = 0f;
        navigation.AvoidRobots = false;
        path.Clear();

        if (!FindPath(entity, position.Cell, destination.Cell, avoid, cells, occupants, path) && avoid)
            Sidestep(entity, position.Cell, cells, occupants, path);
    }

    bool FindPath(Entity entity, int2 start, int2 goal, bool avoid, NativeArray<CellState> cells,
        NativeArray<CellOccupant> occupants, DynamicBuffer<PathNode> path)
    {
        if (start.Equals(goal))
            return true;

        if (!IsWalkable(entity, goal, goal, false, cells, occupants))
            return false;

        int count = Width * Height;
        int startIndex = GridUtils.CellIndex(start, Width);
        int goalIndex = GridUtils.CellIndex(goal, Width);

        var cost = new NativeArray<int>(count, Allocator.Temp);
        var parent = new NativeArray<int>(count, Allocator.Temp);
        var closed = new NativeArray<bool>(count, Allocator.Temp);
        var open = new NativeList<int>(64, Allocator.Temp);

        for (int i = 0; i < count; i++)
            cost[i] = int.MaxValue;

        cost[startIndex] = 0;
        open.Add(startIndex);
        bool found = false;

        while (open.Length > 0)
        {
            int best = 0;
            int bestScore = int.MaxValue;

            for (int i = 0; i < open.Length; i++)
            {
                int score = cost[open[i]] + GridUtils.Distance(GridUtils.IndexToCell(open[i], Width), goal);
                if (score < bestScore)
                {
                    bestScore = score;
                    best = i;
                }
            }

            int current = open[best];
            open.RemoveAtSwapBack(best);

            if (current == goalIndex)
            {
                found = true;
                break;
            }

            if (closed[current])
                continue;

            closed[current] = true;
            int2 cell = GridUtils.IndexToCell(current, Width);

            for (int d = 0; d < 4; d++)
            {
                int2 next = cell + GridUtils.Direction(d);
                if (!IsWalkable(entity, next, goal, avoid, cells, occupants))
                    continue;

                int nextIndex = GridUtils.CellIndex(next, Width);
                if (closed[nextIndex] || cost[current] + 1 >= cost[nextIndex])
                    continue;

                cost[nextIndex] = cost[current] + 1;
                parent[nextIndex] = current;
                open.Add(nextIndex);
            }
        }

        if (found)
        {
            path.ResizeUninitialized(cost[goalIndex]);
            for (int index = goalIndex, i = path.Length - 1; index != startIndex; index = parent[index], i--)
                path[i] = new PathNode { Cell = GridUtils.IndexToCell(index, Width) };
        }

        cost.Dispose();
        parent.Dispose();
        closed.Dispose();
        open.Dispose();
        return found;
    }

    void Sidestep(Entity entity, int2 start, NativeArray<CellState> cells, NativeArray<CellOccupant> occupants,
        DynamicBuffer<PathNode> path)
    {
        int offset = entity.Index + Seed;

        for (int i = 0; i < 4; i++)
        {
            int2 next = start + GridUtils.Direction((i + offset) & 3);
            if (IsWalkable(entity, next, new int2(-1), true, cells, occupants))
            {
                path.Add(new PathNode { Cell = next });
                return;
            }
        }
    }

    bool IsWalkable(Entity entity, int2 cell, int2 goal, bool avoid, NativeArray<CellState> cells,
        NativeArray<CellOccupant> occupants)
    {
        if (math.any(cell < 0) || cell.x >= Width || cell.y >= Height)
            return false;

        int index = GridUtils.CellIndex(cell, Width);
        bool isGoal = cell.Equals(goal);
        var kind = cells[index].Kind;
        bool passable = kind == CellKind.Floor || kind == CellKind.Parking || (kind == CellKind.Charger && isGoal);

        if (!passable)
            return false;

        if (!avoid || isGoal)
            return true;

        var occupant = occupants[index].Robot;
        return occupant == Entity.Null || occupant == entity;
    }
}
