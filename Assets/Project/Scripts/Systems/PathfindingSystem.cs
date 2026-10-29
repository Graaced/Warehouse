using Unity.Burst;
using Unity.Entities;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(MovementSystem))]
[BurstCompile]
public partial struct PathfindingSystem : ISystem
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
        var dimensions = SystemAPI.GetComponent<GridDimensions>(grid);

        state.Dependency = new PathfindingJob
        {
            CellLookup = SystemAPI.GetBufferLookup<CellState>(true),
            OccupantLookup = SystemAPI.GetBufferLookup<CellOccupant>(true),
            Grid = grid,
            Width = dimensions.Width,
            Height = dimensions.Height,
            Seed = (int)(SystemAPI.Time.ElapsedTime * 10)
        }.ScheduleParallel(state.Dependency);
    }
}
