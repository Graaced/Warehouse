using Unity.Entities;
using Unity.Mathematics;

public struct RobotIntent : IComponentData
{
    public const int CarrierPriority = 1;
    public const int EmptyPriority = 0;

    public bool HasNext;
    public int2 Next;
    public int Priority;
}
