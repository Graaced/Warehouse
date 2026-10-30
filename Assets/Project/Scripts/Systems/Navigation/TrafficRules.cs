using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public struct PathQuery
{
    public Entity Robot;
    public int2 Start;
    public int2 Goal;
    public bool Carrying;
    public bool AvoidRobots;
    public bool FromService;
    public bool BlockOccupiedService;
}

// Every movement rule of the warehouse lives here, shared by A* and by the sidestep fallback, so a robot can
// never take a shortcut that the planner would have refused. Hard rules are expressed in CanEnter, soft
// preferences (the traffic flows) in StepCost.
public static class TrafficRules
{
    public static bool CanEnter(ref GridMapBlob map, NativeArray<CellOccupant> occupants, in PathQuery query, int2 from, int2 to)
    {
        if (!map.Contains(to))
            return false;

        var fromKind = map.Kind(from);
        var toKind = map.Kind(to);

        if (!IsPassable(toKind, query, to))
            return false;

        if (!RespectsCounterFlow(fromKind, toKind, from, to))
            return false;

        if (!RespectsCarrierLane(map.Lane(from), map.Lane(to), query.Carrying))
            return false;

        if (!IsOccupiedByOther(ref map, occupants, query.Robot, to))
            return true;

        return !query.AvoidRobots && !(toKind == CellKind.PackingService && query.BlockOccupiedService);
    }

    // Moving against a traffic flow is allowed but expensive, so robots follow the flows when they can and
    // still have an escape route when a lane is jammed.
    public static int StepCost(ref GridMapBlob map, in PathQuery query, int2 from, int2 to, int lanePenalty)
    {
        int dx = to.x - from.x;
        if (dx == 0)
            return 1;

        bool withFlow = map.Lane(to) switch
        {
            CellLane.CenterWest => query.Carrying && dx < 0,
            CellLane.CenterEast => query.Carrying && dx > 0,
            CellLane.Top => !query.Carrying && dx < 0,
            CellLane.Bottom => !query.Carrying && dx > 0,
            _ => true
        };

        return withFlow ? 1 : 1 + lanePenalty;
    }

    // Parking cells are a waiting area that robots with a package must stay out of. Chargers are only entered
    // by the robot that is going to charge there, and counter slots only by the robot that is going to use them
    // or by the robot that is leaving them.
    static bool IsPassable(CellKind kind, in PathQuery query, int2 cell)
    {
        bool isGoal = cell.Equals(query.Goal);

        return kind switch
        {
            CellKind.Floor => true,
            CellKind.Parking => !query.Carrying,
            CellKind.ParkingLane => !query.Carrying,
            CellKind.Charger => isGoal,
            CellKind.PackingService => isGoal || query.FromService,
            _ => false
        };
    }

    // Counter slots are entered from the front and left sideways along the counter: robots leaving a counter
    // never cross the robots that are arriving or waiting in the queue in front of it.
    static bool RespectsCounterFlow(CellKind fromKind, CellKind toKind, int2 from, int2 to)
    {
        bool fromService = fromKind == CellKind.PackingService;
        bool toService = toKind == CellKind.PackingService;

        if (fromService && !toService)
            return to.x == from.x;

        if (toService && !fromService)
            return to.x != from.x;

        return true;
    }

    // The center aisle carries packages only. An empty robot may still leave it if it happens to stand there,
    // but it can never step into it from outside.
    static bool RespectsCarrierLane(CellLane fromLane, CellLane toLane, bool carrying)
    {
        return carrying || !IsCenter(toLane) || IsCenter(fromLane);
    }

    static bool IsCenter(CellLane lane)
    {
        return lane == CellLane.CenterWest || lane == CellLane.CenterEast;
    }

    static bool IsOccupiedByOther(ref GridMapBlob map, NativeArray<CellOccupant> occupants, Entity robot, int2 cell)
    {
        var occupant = occupants[map.Index(cell)].Robot;
        return occupant != Entity.Null && occupant != robot;
    }
}
