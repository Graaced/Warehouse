using Unity.Entities;

public struct RobotAssignment : IComponentData
{
    public Entity ParkingSlot;
    public Entity Station;
    public int StationSlot;
}
