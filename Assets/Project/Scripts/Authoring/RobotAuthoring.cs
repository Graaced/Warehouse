using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

public class RobotAuthoring : MonoBehaviour
{
    public float MetersPerSecond = 4f;
    public float RepathDelay = 1f;
    public GameObject Package;

    [Header("State Colors")]
    public Color ToShelfColor = new Color(0.216f, 0.541f, 0.867f);
    public Color PickingUpColor = new Color(0.29f, 0.565f, 0.886f);
    public Color TransportingColor = new Color(0.937f, 0.624f, 0.153f);
    public Color UnloadingColor = new Color(0.498f, 0.467f, 0.867f);
    public Color LoadingColor = new Color(0.851f, 0.467f, 0.341f);
    public Color ToChargerColor = new Color(0.886f, 0.294f, 0.29f);
    public Color ChargingColor = new Color(0.388f, 0.6f, 0.133f);

    class Baker : Baker<RobotAuthoring>
    {
        public override void Bake(RobotAuthoring authoring)
        {
            var renderer = GetComponent<MeshRenderer>();
            if (renderer == null || renderer.sharedMaterial == null || authoring.Package == null)
            {
                Debug.LogError("[RobotAuthoring] The robot needs a MeshRenderer with a material and a Package child!", authoring);
                return;
            }

            DependsOn(renderer.sharedMaterial);

            var entity = GetEntity(TransformUsageFlags.Dynamic);
            var idleColor = ToLinear(renderer.sharedMaterial.GetColor("_BaseColor"));

            AddComponent(entity, new RobotConfig
            {
                MetersPerSecond = authoring.MetersPerSecond,
                RepathDelay = authoring.RepathDelay
            });

            AddComponent<RobotState>(entity);
            AddComponent<RobotPosition>(entity);
            AddComponent<RobotDestination>(entity);
            AddComponent<RobotNavigation>(entity);
            AddComponent<RobotIntent>(entity);
            AddBuffer<PathNode>(entity);
            AddComponent<PathRequest>(entity);
            SetComponentEnabled<PathRequest>(entity, false);

            AddComponent<ActionTimer>(entity);
            AddComponent<CurrentOrder>(entity);
            AddComponent<RobotAssignment>(entity);
            AddComponent<RobotVisual>(entity);

            AddComponent(entity, new URPMaterialPropertyBaseColor { Value = idleColor });

            AddComponent(entity, new RobotPalette
            {
                Idle = idleColor,
                ToShelf = ToLinear(authoring.ToShelfColor),
                PickingUp = ToLinear(authoring.PickingUpColor),
                Transporting = ToLinear(authoring.TransportingColor),
                Unloading = ToLinear(authoring.UnloadingColor),
                Loading = ToLinear(authoring.LoadingColor),
                ToCharger = ToLinear(authoring.ToChargerColor),
                Charging = ToLinear(authoring.ChargingColor)
            });

            AddComponent(entity, new RobotPackage
            {
                Value = GetEntity(authoring.Package, TransformUsageFlags.Dynamic),
                Scale = authoring.Package.transform.localScale.x
            });
        }

        // URPMaterialPropertyBaseColor is uploaded to the GPU as-is, while material colors are authored in sRGB.
        static float4 ToLinear(Color color)
        {
            var linear = color.linear;
            return new float4(linear.r, linear.g, linear.b, linear.a);
        }
    }
}
