using Unity.Burst;
using Unity.Entities;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[BurstCompile]
public partial struct MovementSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<TagGrid>();
        state.RequireForUpdate<TagRobot>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var grid = SystemAPI.GetSingletonEntity<TagGrid>();

        state.Dependency = new IntentJob().ScheduleParallel(state.Dependency);

        state.Dependency = new MovementJob
        {
            OccupantLookup = SystemAPI.GetBufferLookup<CellOccupant>(),
            IntentLookup = SystemAPI.GetComponentLookup<RobotIntent>(true),
            Grid = grid,
            Width = SystemAPI.GetComponent<GridDimensions>(grid).Width,
            DeltaTime = SystemAPI.Time.DeltaTime
        }.Schedule(state.Dependency);
    }
}
