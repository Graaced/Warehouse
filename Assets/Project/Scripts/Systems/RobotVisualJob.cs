using Unity.Burst;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;

[BurstCompile]
public partial struct RobotVisualJob : IJobEntity
{
    public ComponentLookup<LocalTransform> TransformLookup;

    public void Execute(ref URPMaterialPropertyBaseColor color, in RobotState state, in RobotPalette palette, in RobotPackage package)
    {
        color.Value = palette.Get(state.Value);

        var transform = TransformLookup[package.Value];
        transform.Scale = state.IsCarrying ? package.Scale : 0f;
        TransformLookup[package.Value] = transform;
    }
}
