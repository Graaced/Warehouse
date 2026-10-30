using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[BurstCompile]
[WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)]
public partial struct PathfindingJob : IJobEntity
{
    const int LanePenalty = 3;

    struct Request
    {
        public NativeArray<CellState> Cells;
        public NativeArray<CellOccupant> Occupants;
        public Entity Robot;
        public int2 Start;
        public int2 Goal;
        public bool Avoid;
        public bool Carrying;
        public bool FromService;
        public bool BlockOccupiedService;
    }

    [ReadOnly] public BufferLookup<CellState> CellLookup;
    [ReadOnly] public BufferLookup<CellOccupant> OccupantLookup;
    public Entity Grid;
    public int Width;
    public int Height;
    public int Seed;

    public void Execute(Entity entity, ref RobotNavigation navigation, in RobotPosition position, in RobotDestination destination,
        in RobotState robotState, DynamicBuffer<PathNode> path, EnabledRefRW<PathRequest> request)
    {
        if (!request.ValueRO || navigation.Moving)
            return;

        var cells = CellLookup[Grid].AsNativeArray();

        var context = new Request
        {
            Cells = cells,
            Occupants = OccupantLookup[Grid].AsNativeArray(),
            Robot = entity,
            Start = position.Cell,
            Goal = destination.Cell,
            Avoid = navigation.AvoidRobots,
            Carrying = robotState.IsCarrying,
            FromService = cells[GridUtils.CellIndex(position.Cell, Width)].Kind == CellKind.PackingService
        };

        request.ValueRW = false;
        navigation.PathIndex = 0;
        navigation.BlockedTime = 0f;
        navigation.AvoidRobots = false;
        path.Clear();

        context.BlockOccupiedService = context.FromService;
        bool found = FindPath(context, path);

        if (!found && context.FromService)
        {
            context.BlockOccupiedService = false;
            found = FindPath(context, path);
        }

        if (!found && context.Avoid)
            Sidestep(context, path);
    }

    bool FindPath(Request context, DynamicBuffer<PathNode> path)
    {
        if (context.Start.Equals(context.Goal))
            return true;

        int count = Width * Height;
        int startIndex = GridUtils.CellIndex(context.Start, Width);
        int goalIndex = GridUtils.CellIndex(context.Goal, Width);

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
                int score = cost[open[i]] + GridUtils.Distance(GridUtils.IndexToCell(open[i], Width), context.Goal);
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
                if (!CanEnter(context, cell, next))
                    continue;

                int nextIndex = GridUtils.CellIndex(next, Width);
                int nextCost = cost[current] + StepCost(context, cell, next);
                if (closed[nextIndex] || nextCost >= cost[nextIndex])
                    continue;

                cost[nextIndex] = nextCost;
                parent[nextIndex] = current;
                open.Add(nextIndex);
            }
        }

        if (found)
        {
            for (int index = goalIndex; index != startIndex; index = parent[index])
                path.Add(new PathNode { Cell = GridUtils.IndexToCell(index, Width) });

            for (int i = 0, j = path.Length - 1; i < j; i++, j--)
            {
                var swap = path[i];
                path[i] = path[j];
                path[j] = swap;
            }
        }

        cost.Dispose();
        parent.Dispose();
        closed.Dispose();
        open.Dispose();
        return found;
    }

    void Sidestep(Request context, DynamicBuffer<PathNode> path)
    {
        context.Goal = new int2(-1);
        int offset = context.Robot.Index + Seed;

        for (int i = 0; i < 4; i++)
        {
            int2 next = context.Start + GridUtils.Direction((i + offset) & 3);
            if (CanEnter(context, context.Start, next))
            {
                path.Add(new PathNode { Cell = next });
                return;
            }
        }
    }

    bool CanEnter(Request context, int2 from, int2 to)
    {
        if (math.any(to < 0) || to.x >= Width || to.y >= Height)
            return false;

        bool isGoal = to.Equals(context.Goal);
        var kind = Kind(context, to);

        bool passable = kind switch
        {
            CellKind.Floor => true,
            CellKind.Parking => !context.Carrying,
            CellKind.ParkingLane => !context.Carrying,
            CellKind.Charger => isGoal,
            CellKind.PackingService => isGoal || context.FromService,
            _ => false
        };

        if (!passable)
            return false;

        bool fromService = Kind(context, from) == CellKind.PackingService;
        bool toService = kind == CellKind.PackingService;

        if (fromService && !toService && to.x != from.x)
            return false;

        if (toService && !fromService && to.x == from.x)
            return false;

        if (toService && context.BlockOccupiedService && IsOccupied(context, to))
            return false;

        if (!context.Carrying && IsCenter(Lane(context, to)) && !IsCenter(Lane(context, from)))
            return false;

        return !context.Avoid || !IsOccupied(context, to);
    }

    int StepCost(Request context, int2 from, int2 to)
    {
        int dx = to.x - from.x;
        if (dx == 0)
            return 1;

        bool withFlow = Lane(context, to) switch
        {
            CellLane.CenterWest => context.Carrying && dx < 0,
            CellLane.CenterEast => context.Carrying && dx > 0,
            CellLane.Top => !context.Carrying && dx < 0,
            CellLane.Bottom => !context.Carrying && dx > 0,
            _ => true
        };

        return withFlow ? 1 : 1 + LanePenalty;
    }

    bool IsOccupied(Request context, int2 cell)
    {
        var occupant = context.Occupants[GridUtils.CellIndex(cell, Width)].Robot;
        return occupant != Entity.Null && occupant != context.Robot;
    }

    CellKind Kind(Request context, int2 cell)
    {
        return context.Cells[GridUtils.CellIndex(cell, Width)].Kind;
    }

    CellLane Lane(Request context, int2 cell)
    {
        return context.Cells[GridUtils.CellIndex(cell, Width)].Lane;
    }

    static bool IsCenter(CellLane lane)
    {
        return lane == CellLane.CenterWest || lane == CellLane.CenterEast;
    }
}
