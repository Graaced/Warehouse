using Unity.Entities;

public struct RobotBattery : IComponentData
{
    public const float Full = 100f;

    public float Level;
}
