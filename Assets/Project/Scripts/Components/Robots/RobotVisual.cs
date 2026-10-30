using Unity.Entities;

public struct RobotVisual : IComponentData
{
    public RobotStateKind Shown;
    public bool Applied;
    public float ChargingLift;
}
