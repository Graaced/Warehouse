using Unity.Entities;

public struct RobotNavigation : IComponentData
{
    public int PathIndex;
    public bool Moving;
    public bool AvoidRobots;
    public float BlockedTime;
}
