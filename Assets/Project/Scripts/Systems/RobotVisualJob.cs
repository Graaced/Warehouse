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

        bool carrying = state.Value == RobotStateKind.Transporting ||
                        state.Value == RobotStateKind.Unloading ||
                        state.Value == RobotStateKind.Loading;

        var transform = TransformLookup[package.Value];
        transform.Scale = carrying ? package.Scale : 0f;
        TransformLookup[package.Value] = transform;
    }
}
