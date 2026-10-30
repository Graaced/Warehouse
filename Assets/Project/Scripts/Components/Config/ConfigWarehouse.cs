using Unity.Entities;

public struct ConfigWarehouse : IComponentData
{
    public int ShelfColumnPairs;
    public int TopBlockRows;
    public int BottomBlockRows;
    public int AisleWidth;
    public int SideAisleWidth;

    public int StationLength;
    public int PackingStationCount;
    public int ChargerCount;
    public int LoadingBayCount;

    public int MaxRobots;
    public int InitialRobots;

    public int OrdersPerMinute;
    public int MaxPendingOrders;

    public float LiftSeconds;
    public float PackagingSeconds;
    public float DeliverySeconds;
    public float RespawnMinSeconds;
    public float RespawnMaxSeconds;

    public float MinInitialBattery;
    public float BatteryPerCell;
    public float LowBattery;
    public float ChargePerSecond;

    public int ApproachDistance;
    public int StationLoadWeight;
    public int LanePenalty;

    public Entity Robot;
    public Entity Shelf;
    public Entity PackingStation;
    public Entity Charger;
    public Entity LoadingBay;
    public Entity ParkingSlot;
    public Entity FloorTile;
}
