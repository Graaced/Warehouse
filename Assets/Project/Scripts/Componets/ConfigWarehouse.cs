using Unity.Entities;

public struct ConfigWarehouse : IComponentData
{
    public int ShelfColumnPairs;
    public int TopBlockRows;
    public int BottomBlockRows;
    public int AisleWidth;
    public int SideAisleWidth;

    public int PackingStationCount;
    public int PackingStationLength;
    public int ChargerCount;
    public int LoadingBayCount;

    public int MaxRobots;
    public int InitialRobots;

    public Entity Robot;
    public Entity Shelf;
    public Entity PackingStation;
    public Entity Charger;
    public Entity LoadingBay;
    public Entity ParkingSlot;
    public Entity FloorTile;
}
