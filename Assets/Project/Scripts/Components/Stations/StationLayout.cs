using Unity.Entities;
using Unity.Mathematics;

public enum StationKind : byte
{
    Packing,
    Loading
}

public struct StationLayout : IComponentData
{
    public StationKind Kind;
    public int2 Center;
    public int2 Approach;
}
