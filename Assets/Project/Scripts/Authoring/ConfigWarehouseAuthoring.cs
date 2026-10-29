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

    [Range(1, 6)]
    public int AisleWidth = 4;

    [Range(2, 12)]
    public int SideAisleWidth = 8;

    [Header("Stations")]
    [Range(1, 10)]
    public int PackingStationCount = 4;

    [Range(1, 7)]
    public int PackingStationLength = 3;

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

    [Header("Timings (seconds)")]
    public float LiftSeconds = 0.4f;
    public float RespawnMinSeconds = 5f;
    public float RespawnMaxSeconds = 15f;

    [Header("Prefab")]
    public GameObject Robot;
    public GameObject Shelf;
    public GameObject PackingStation;
    public GameObject Charger;
    public GameObject LoadingBay;
    public GameObject ParkingSlot;
    public GameObject FloorTile;
}
