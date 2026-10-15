using Unity.Entities;
using Unity.Mathematics;

public struct RobotMovement : IComponentData
{
    public int2 CurrentCell;   // current cell on the grid
    public int2 TargetCell;    // final destination cell
    public float MoveSpeed;    // units (cells) per second
}
