using Unity.Entities;

public enum StationStage : byte
{
    None,
    Approaching,
    ToQueue,
    InQueue,
    Serving
}

// The robot is the single owner of its reservations. Who occupies a parking slot or a station slot
// is rebuilt every frame from these values, so a removed robot can never leave a stale reservation behind.
public struct RobotAssignment : IComponentData
{
    public Entity ParkingSlot;
    public Entity Station;
    public StationStage Stage;
    public int Slot;
    public double QueuedAt;
}
