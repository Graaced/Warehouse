using Unity.Entities;

public enum OrderStateKind : byte
{
    Queued,
    Assigned,
    Packaged,
    Delivered
}

public struct OrderState : IComponentData
{
    public OrderStateKind Value;
}
