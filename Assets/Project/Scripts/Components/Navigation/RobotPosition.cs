using Unity.Entities;
using Unity.Mathematics;

public struct RobotPosition : IComponentData
{
    public int2 Cell;
}
