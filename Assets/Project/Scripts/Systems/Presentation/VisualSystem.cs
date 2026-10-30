using Unity.Burst;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;

[UpdateInGroup(typeof(WarehouseVisualSystemGroup))]
[BurstCompile]
public partial struct VisualSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        state.Dependency = new ShelfVisualJob
        {
            TransformLookup = SystemAPI.GetComponentLookup<LocalTransform>()
        }.ScheduleParallel(state.Dependency);

        state.Dependency = new RobotVisualJob
        {
            ColorLookup = SystemAPI.GetComponentLookup<URPMaterialPropertyBaseColor>(),
            TransformLookup = SystemAPI.GetComponentLookup<LocalTransform>()
        }.ScheduleParallel(state.Dependency);
    }
}
