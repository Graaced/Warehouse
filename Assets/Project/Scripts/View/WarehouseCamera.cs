using Unity.Entities;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class WarehouseCamera : MonoBehaviour
{
    public float Margin = 1f;
    public Color Background = new Color(0.54f, 0.53f, 0.5f);

    Camera cam;
    EntityQuery gridQuery;

    void Start()
    {
        cam = GetComponent<Camera>();
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Background;
        transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        gridQuery = World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntityQuery(typeof(GridDimensions));
    }

    void LateUpdate()
    {
        if (!gridQuery.TryGetSingleton(out GridDimensions grid))
            return;

        float halfWidth = grid.Width * 0.5f + Margin;
        float halfHeight = grid.Height * 0.5f + Margin;
        cam.orthographicSize = Mathf.Max(halfHeight, halfWidth / cam.aspect);
        transform.position = new Vector3((grid.Width - 1) * 0.5f, 50f, (grid.Height - 1) * 0.5f);
    }
}
