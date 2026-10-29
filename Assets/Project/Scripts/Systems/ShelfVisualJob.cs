using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

[BurstCompile]
public partial struct ShelfVisualJob : IJobEntity
{
    [ReadOnly] public BufferLookup<CellState> CellLookup;
    public ComponentLookup<LocalTransform> TransformLookup;
    public Entity Grid;
    public int Width;

    public void Execute(in ShelfCell shelf, in ShelfPackage package)
    {
        var fill = CellLookup[Grid][GridUtils.CellIndex(shelf.Cell, Width)].Fill;
        var transform = TransformLookup[package.Value];
        transform.Scale = fill == CellFill.Full ? package.Scale : 0f;
        TransformLookup[package.Value] = transform;
    }
}
