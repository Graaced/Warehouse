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

        bool wasCharging = visual.Applied && visual.Shown == RobotStateKind.Charging;
        bool isCharging = state.Value == RobotStateKind.Charging;

        visual.Shown = state.Value;
        visual.Applied = true;

        ColorLookup[entity] = new URPMaterialPropertyBaseColor { Value = palette.Get(state.Value) };

        // The robot climbs onto the charger platform. Movement keeps the current height, so the lift has to be
        // removed again before the robot drives away.
        if (wasCharging != isCharging)
        {
            var robot = TransformLookup[entity];
            robot.Position.y += isCharging ? visual.ChargingLift : -visual.ChargingLift;
            TransformLookup[entity] = robot;
        }

        var transform = TransformLookup[package.Value];
        transform.Scale = state.IsCarrying ? package.Scale : 0f;
        TransformLookup[package.Value] = transform;
    }
}
