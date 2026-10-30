using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;

// Writes the color and the carried package only when the state actually changed: writing every frame would
// bump the change versions and force the renderer and the transform systems to reprocess every robot.
[BurstCompile]
public partial struct RobotVisualJob : IJobEntity
{
    [NativeDisableParallelForRestriction] public ComponentLookup<URPMaterialPropertyBaseColor> ColorLookup;
    [NativeDisableParallelForRestriction] public ComponentLookup<LocalTransform> TransformLookup;

    public void Execute(Entity entity, ref RobotVisual visual, in RobotState state, in RobotPalette palette, in RobotPackage package)
    {
        if (visual.Applied && visual.Shown == state.Value)
            return;

        visual.Shown = state.Value;
        visual.Applied = true;

        ColorLookup[entity] = new URPMaterialPropertyBaseColor { Value = palette.Get(state.Value) };

        var transform = TransformLookup[package.Value];
        transform.Scale = state.IsCarrying ? package.Scale : 0f;
        TransformLookup[package.Value] = transform;
    }
}
