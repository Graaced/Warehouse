using Unity.Entities;
using Unity.Mathematics;

public struct PathNode : IBufferElementData
{
    public int2 Cell;
}
