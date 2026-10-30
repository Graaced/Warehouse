using Unity.Entities;

public struct ShelfStock : IComponentData
{
    public bool Empty;
    public double RespawnAt;
}
