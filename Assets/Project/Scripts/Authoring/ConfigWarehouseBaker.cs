using Unity.Entities;
using UnityEngine;

public class ConfigWarehouseBaker : Baker<ConfigWarehouseAuthoring>
{
    public override void Bake(ConfigWarehouseAuthoring authoring)
    {
        if (authoring.InitialRobots > authoring.MaxRobots)
        {
            Debug.LogError("[ConfigWarehouse] InitialRobots can't exceed MaxRobots!", authoring);
            return;
        }

        if (authoring.StationLength % 2 == 0)
        {
            Debug.LogError("[ConfigWarehouse] StationLength must be odd!", authoring);
            return;
        }

        if (authoring.SideAisleWidth < 5)
        {
            Debug.LogError("[ConfigWarehouse] SideAisleWidth must be at least 5 to fit the packing queues!", authoring);
            return;
        }

        if (authoring.Robot == null || authoring.Shelf == null || authoring.PackingStation == null ||
            authoring.Charger == null || authoring.LoadingBay == null || authoring.ParkingSlot == null ||
            authoring.FloorTile == null)
        {
            Debug.LogError("[ConfigWarehouse] All prefabs must be assigned!", authoring);
            return;
        }

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
            LiftSeconds = authoring.LiftSeconds,
            PackagingSeconds = authoring.PackagingSeconds,
            RespawnMinSeconds = authoring.RespawnMinSeconds,
            RespawnMaxSeconds = authoring.RespawnMaxSeconds,
            Robot = GetEntity(authoring.Robot, TransformUsageFlags.Dynamic),
            Shelf = GetEntity(authoring.Shelf, TransformUsageFlags.Dynamic),
            PackingStation = GetEntity(authoring.PackingStation, TransformUsageFlags.Dynamic),
            Charger = GetEntity(authoring.Charger, TransformUsageFlags.Dynamic),
            LoadingBay = GetEntity(authoring.LoadingBay, TransformUsageFlags.Dynamic),
            ParkingSlot = GetEntity(authoring.ParkingSlot, TransformUsageFlags.Dynamic),
            FloorTile = GetEntity(authoring.FloorTile, TransformUsageFlags.Dynamic),
        });
    }
}
