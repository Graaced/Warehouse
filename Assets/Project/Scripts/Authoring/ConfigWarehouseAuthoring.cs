using Unity.Entities;
using UnityEngine;

public class ConfigWarehouseAuthoring : MonoBehaviour
{
    [Header("Shelves Layout")]
    [Range(1, 20)]
    public int ShelfColumnPairs = 6;

    [Range(1, 20)]
    public int TopBlockRows = 6;

    [Range(1, 20)]
    public int BottomBlockRows = 7;

    [Range(1, 8)]
    public int AisleWidth = 6;

    [Range(2, 14)]
    public int SideAisleWidth = 10;

    [Header("Stations")]
    [Range(1, 7)]
    public int StationLength = 3;

    [Range(1, 10)]
    public int PackingStationCount = 4;

    [Range(1, 10)]
    public int ChargerCount = 5;

    [Range(1, 10)]
    public int LoadingBayCount = 3;

    [Header("Robots")]
    [Range(1, 200)]
    public int MaxRobots = 60;

    [Range(1, 200)]
    public int InitialRobots = 18;

    [Header("Orders")]
    [Range(10, 300)]
    public int OrdersPerMinute = 90;

    [Range(10, 2000)]
    public int MaxPendingOrders = 500;

    [Header("Timings (seconds)")]
    public float LiftSeconds = 0.4f;
    public float PackagingSeconds = 1.5f;
    public float DeliverySeconds = 0.5f;
    public float RespawnMinSeconds = 5f;
    public float RespawnMaxSeconds = 15f;

    [Header("Battery (percent)")]
    [Range(0, 100)]
    public float MinInitialBattery = 40f;

    [Range(0, 100)]
    public float LowBattery = 25f;

    public float BatteryPerCell = 0.35f;
    public float ChargePerSecond = 30f;

    [Header("Traffic")]
    [Range(2, 20)]
    public int ApproachDistance = 6;

    [Range(0, 20)]
    public int StationLoadWeight = 4;

    [Range(0, 20)]
    public int LanePenalty = 3;

    [Header("Prefab")]
    public GameObject Robot;
    public GameObject Shelf;
    public GameObject PackingStation;
    public GameObject Charger;
    public GameObject LoadingBay;
    public GameObject ParkingSlot;
    public GameObject FloorTile;

    class Baker : Baker<ConfigWarehouseAuthoring>
    {
        public override void Bake(ConfigWarehouseAuthoring authoring)
        {
            if (!IsValid(authoring))
                return;

            var entity = GetEntity(TransformUsageFlags.None);

            AddComponent(entity, new ConfigWarehouse
            {
                ShelfColumnPairs = authoring.ShelfColumnPairs,
                TopBlockRows = authoring.TopBlockRows,
                BottomBlockRows = authoring.BottomBlockRows,
                AisleWidth = authoring.AisleWidth,
                SideAisleWidth = authoring.SideAisleWidth,
                StationLength = authoring.StationLength,
                PackingStationCount = authoring.PackingStationCount,
                ChargerCount = authoring.ChargerCount,
                LoadingBayCount = authoring.LoadingBayCount,
                MaxRobots = authoring.MaxRobots,
                InitialRobots = authoring.InitialRobots,
                OrdersPerMinute = authoring.OrdersPerMinute,
                MaxPendingOrders = authoring.MaxPendingOrders,
                LiftSeconds = authoring.LiftSeconds,
                PackagingSeconds = authoring.PackagingSeconds,
                DeliverySeconds = authoring.DeliverySeconds,
                RespawnMinSeconds = authoring.RespawnMinSeconds,
                RespawnMaxSeconds = authoring.RespawnMaxSeconds,
                MinInitialBattery = authoring.MinInitialBattery,
                BatteryPerCell = authoring.BatteryPerCell,
                LowBattery = authoring.LowBattery,
                ChargePerSecond = authoring.ChargePerSecond,
                ApproachDistance = authoring.ApproachDistance,
                StationLoadWeight = authoring.StationLoadWeight,
                LanePenalty = authoring.LanePenalty,
                Robot = GetEntity(authoring.Robot, TransformUsageFlags.Dynamic),
                Shelf = GetEntity(authoring.Shelf, TransformUsageFlags.Dynamic),
                PackingStation = GetEntity(authoring.PackingStation, TransformUsageFlags.Dynamic),
                Charger = GetEntity(authoring.Charger, TransformUsageFlags.Dynamic),
                LoadingBay = GetEntity(authoring.LoadingBay, TransformUsageFlags.Dynamic),
                ParkingSlot = GetEntity(authoring.ParkingSlot, TransformUsageFlags.Dynamic),
                FloorTile = GetEntity(authoring.FloorTile, TransformUsageFlags.Dynamic)
            });
        }

        static bool IsValid(ConfigWarehouseAuthoring authoring)
        {
            if (authoring.InitialRobots > authoring.MaxRobots)
                return Fail(authoring, "InitialRobots can't exceed MaxRobots!");

            if (authoring.StationLength % 2 == 0)
                return Fail(authoring, "StationLength must be odd!");

            if (authoring.SideAisleWidth < WarehouseLayout.MinSideAisleWidth)
                return Fail(authoring, $"SideAisleWidth must be at least {WarehouseLayout.MinSideAisleWidth} to fit the station queues!");

            if (authoring.RespawnMinSeconds > authoring.RespawnMaxSeconds)
                return Fail(authoring, "RespawnMinSeconds can't exceed RespawnMaxSeconds!");

            if (authoring.Robot == null || authoring.Shelf == null || authoring.PackingStation == null ||
                authoring.Charger == null || authoring.LoadingBay == null || authoring.ParkingSlot == null ||
                authoring.FloorTile == null)
                return Fail(authoring, "All prefabs must be assigned!");

            return true;
        }

        static bool Fail(ConfigWarehouseAuthoring authoring, string message)
        {
            Debug.LogError($"[ConfigWarehouse] {message}", authoring);
            return false;
        }
    }
}
