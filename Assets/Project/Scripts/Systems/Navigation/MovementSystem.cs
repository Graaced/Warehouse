using Unity.Burst;
using Unity.Entities;

[UpdateInGroup(typeof(NavigationSystemGroup))]
[UpdateAfter(typeof(PathfindingSystem))]
[BurstCompile]
public partial struct MovementSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<ConfigWarehouse>();
        state.RequireForUpdate<GridMap>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var grid = SystemAPI.GetSingletonEntity<GridMap>();

        state.Dependency = new IntentJob().ScheduleParallel(state.Dependency);

        // Carriers claim free cells first: when a carrier and an empty robot want the same cell in the same
        // frame, the carrier always gets it.
        var movement = new MovementJob
        {
            OccupantLookup = SystemAPI.GetBufferLookup<CellOccupant>(),
            IntentLookup = SystemAPI.GetComponentLookup<RobotIntent>(true),
            Map = SystemAPI.GetComponent<GridMap>(grid).Value,
            Grid = grid,
            DeltaTime = SystemAPI.Time.DeltaTime,
            BatteryPerCell = SystemAPI.GetSingleton<ConfigWarehouse>().BatteryPerCell
        };

        movement.Priority = RobotIntent.CarrierPriority;
        state.Dependency = movement.Schedule(state.Dependency);

        movement.Priority = RobotIntent.EmptyPriority;
        state.Dependency = movement.Schedule(state.Dependency);
    }
}
