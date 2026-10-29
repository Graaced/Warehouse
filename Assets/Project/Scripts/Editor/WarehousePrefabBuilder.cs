using UnityEditor;
using UnityEngine;

public static class WarehousePrefabBuilder
{
    const string MaterialFolder = "Assets/Project/Materials/Warehouse";
    const string PrefabFolder = "Assets/Project/Prefabs/Warehouse";

    [MenuItem("Warehouse/Create Prefabs")]
    public static void CreatePrefabs()
    {
        EnsureFolder("Assets/Project/Materials", "Warehouse");
        EnsureFolder("Assets/Project/Prefabs", "Warehouse");

        var floor = CreateMaterial("M_Floor", "#E4E2DD");
        var shelf = CreateMaterial("M_ShelfRack", "#4F5A63");
        var package = CreateMaterial("M_Package", "#B5875A");
        var station = CreateMaterial("M_PackingStation", "#6A62B0");
        var charger = CreateMaterial("M_Charger", "#4F7D2A");
        var bay = CreateMaterial("M_LoadingBay", "#B5603F");
        var parking = CreateMaterial("M_ParkingSlot", "#BDBAB3");
        var robot = CreateMaterial("M_Robot", "#6E7275");
        var eye = CreateMaterial("M_RobotEye", "#F2F2F2");

        BuildRobot(robot, eye, package);
        BuildShelf(shelf, package);
        BuildSimple<PackingStationAuthoring>("PackingStation", new Vector3(0f, 0.45f, 0f), new Vector3(0.9f, 0.9f, 3f), station);
        BuildSimple<ChargerAuthoring>("Charger", new Vector3(0f, 0.03f, 0f), new Vector3(0.9f, 0.06f, 0.9f), charger);
        BuildSimple<LoadingBayAuthoring>("LoadingBay", new Vector3(0f, 0.25f, 0f), new Vector3(0.9f, 0.5f, 0.9f), bay);
        BuildSimple<ParkingSlotAuthoring>("ParkingSlot", new Vector3(0f, 0.01f, 0f), new Vector3(0.8f, 0.02f, 0.8f), parking);

        var tile = CreatePart("FloorTile", null, new Vector3(0f, -0.05f, 0f), new Vector3(0.95f, 0.1f, 0.95f), floor);
        Save(tile);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    static void BuildRobot(Material body, Material eye, Material package)
    {
        var robot = CreatePart("Robot", null, new Vector3(0f, 0.35f, 0f), Vector3.one * 0.7f, body);
        CreatePart("EyeLeft", robot.transform, new Vector3(-0.2f, 0.5f, 0.3f), new Vector3(0.14f, 0.04f, 0.14f), eye);
        CreatePart("EyeRight", robot.transform, new Vector3(0.2f, 0.5f, 0.3f), new Vector3(0.14f, 0.04f, 0.14f), eye);

        var carried = CreatePart("Package", robot.transform, new Vector3(0f, 0.78f, -0.12f), Vector3.one * 0.55f, package);
        carried.SetActive(false);

        robot.AddComponent<RobotAuthoring>().Package = carried;
        Save(robot);
    }

    static void BuildShelf(Material rack, Material package)
    {
        var shelf = new GameObject("Shelf");
        CreatePart("Rack", shelf.transform, new Vector3(0f, 0.5f, 0f), new Vector3(0.9f, 1f, 0.9f), rack);
        var stored = CreatePart("Package", shelf.transform, new Vector3(0f, 1.25f, 0f), Vector3.one * 0.5f, package);

        shelf.AddComponent<ShelfAuthoring>().Package = stored;
        Save(shelf);
    }

    static void BuildSimple<T>(string name, Vector3 position, Vector3 scale, Material material) where T : MonoBehaviour
    {
        var root = CreatePart(name, null, position, scale, material);
        root.AddComponent<T>();
        Save(root);
    }

    static GameObject CreatePart(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        var part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = name;
        Object.DestroyImmediate(part.GetComponent<Collider>());
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        part.transform.localScale = scale;
        part.GetComponent<MeshRenderer>().sharedMaterial = material;
        return part;
    }

    static Material CreateMaterial(string name, string hex)
    {
        var path = $"{MaterialFolder}/{name}.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(material, path);
        }

        ColorUtility.TryParseHtmlString(hex, out var color);
        material.SetColor("_BaseColor", color);
        EditorUtility.SetDirty(material);
        return material;
    }

    static void EnsureFolder(string parent, string name)
    {
        if (!AssetDatabase.IsValidFolder($"{parent}/{name}"))
            AssetDatabase.CreateFolder(parent, name);
    }

    static void Save(GameObject root)
    {
        PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabFolder}/{root.name}.prefab");
        Object.DestroyImmediate(root);
    }
}
