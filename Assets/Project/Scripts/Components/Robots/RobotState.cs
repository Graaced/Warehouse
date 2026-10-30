using Unity.Entities;

public enum RobotStateKind : byte
{
    Idle,
    ToShelf,
    PickingUp,
    Transporting,
    Unloading,
    Loading,
    ToCharger,
    Charging
}

public struct RobotState : IComponentData
{
    public RobotStateKind Value;

    public bool IsCarrying => Value == RobotStateKind.Transporting ||
                              Value == RobotStateKind.Unloading ||
                              Value == RobotStateKind.Loading;
}
