using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

[BurstCompile]
public partial struct ShelfVisualJob : IJobEntity
{
    [NativeDisableParallelForRestriction] public ComponentLookup<LocalTransform> TransformLookup;

    public void Execute(ref ShelfVisual visual, in ShelfStock stock, in ShelfPackage package)
    {
        if (visual.Applied && visual.ShownEmpty == stock.Empty)
            return;

        visual.ShownEmpty = stock.Empty;
        visual.Applied = true;

        var transform = TransformLookup[package.Value];
        transform.Scale = stock.Empty ? 0f : package.Scale;
        TransformLookup[package.Value] = transform;
    }
}
