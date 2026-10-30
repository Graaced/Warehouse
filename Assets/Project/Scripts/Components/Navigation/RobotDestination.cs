using Unity.Entities;
using Unity.Mathematics;

public struct RobotDestination : IComponentData
{
    public int2 Cell;
}
