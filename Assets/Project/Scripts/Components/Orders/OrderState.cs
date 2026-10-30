using Unity.Entities;

public enum OrderStateKind : byte
{
    Queued,
    Assigned,
    PickedUp,
    Packaged,
    Delivered
}

public struct OrderState : IComponentData
{
    public OrderStateKind Value;
}
