using Unity.Entities;

public struct RobotConfig : IComponentData
{
    public float MetersPerSecond;
    public float RepathDelay;
}
