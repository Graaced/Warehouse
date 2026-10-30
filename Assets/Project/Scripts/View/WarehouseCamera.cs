using Unity.Entities;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class WarehouseCamera : MonoBehaviour
{
    public float Margin = 1f;
    public Color Background = new Color(0.54f, 0.53f, 0.5f);

    Camera cam;
    EntityQuery mapQuery;

    void Start()
    {
        cam = GetComponent<Camera>();
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Background;
        transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        mapQuery = World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntityQuery(typeof(GridMap));
    }

    void LateUpdate()
    {
        if (!mapQuery.TryGetSingleton(out GridMap map))
            return;

        int width = map.Value.Value.Width;
        int height = map.Value.Value.Height;

        float halfWidth = width * 0.5f + Margin;
        float halfHeight = height * 0.5f + Margin;
        cam.orthographicSize = Mathf.Max(halfHeight, halfWidth / cam.aspect);
        transform.position = new Vector3((width - 1) * 0.5f, 50f, (height - 1) * 0.5f);
    }
}
