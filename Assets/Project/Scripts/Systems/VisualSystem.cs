using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(MovementSystem))]
[BurstCompile]
public partial struct VisualSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<TagGrid>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var grid = SystemAPI.GetSingletonEntity<TagGrid>();

        state.Dependency = new ShelfVisualJob
        {
            CellLookup = SystemAPI.GetBufferLookup<CellState>(true),
            TransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(),
            Grid = grid,
            Width = SystemAPI.GetComponent<GridDimensions>(grid).Width
        }.Schedule(state.Dependency);

        state.Dependency = new RobotVisualJob
        {
            TransformLookup = SystemAPI.GetComponentLookup<LocalTransform>()
        }.Schedule(state.Dependency);
    }
}
