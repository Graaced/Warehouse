using Unity.Entities;
using Unity.Mathematics;

public struct ShelfCell : IComponentData
{
    public int2 Cell;
    public int2 Access;
}
