using Unity.Entities;
using Unity.Mathematics;

public struct StationLayout : IComponentData
{
    public int2 Center;
    public int2 Approach;
}
