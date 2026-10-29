using Unity.Entities;
using Unity.Mathematics;

public struct RobotIntent : IComponentData
{
    public bool HasNext;
    public int2 Next;
    public int Priority;
}
