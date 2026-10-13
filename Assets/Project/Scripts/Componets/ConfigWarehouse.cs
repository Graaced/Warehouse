using Unity.Entities;

public struct ConfigWarehouse : IComponentData
{
    public int RobotCount;
    public int ShelfCount;
    public int GridWidth;
    public int GridHeight;

    public Entity Robot;
    public Entity Shelf;
    public Entity PackingStation;
}
