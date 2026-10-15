using UnityEngine;

public class ConfigWarehouseAuthoring : MonoBehaviour
{
    [Header("WareHouse Configuration")]
    [Range(1, 10000)]
    public int RobotCount = 5;

    [Range(1, 50)]
    public int ShelfCount = 10;

    [Header("Grid Configuration")]
    [Range(2, 50)]
    public int GridWidth = 20;

    [Range(2, 50)]
    public int GridHeight = 20;

    [Header("Prefab")]
    public GameObject Robot;
    public GameObject Shelf;
    public GameObject PackingStation;
}
