using Unity.Entities;

public struct ChargerState : IComponentData
{
    public bool Occupied;
    public Entity Robot;
}
