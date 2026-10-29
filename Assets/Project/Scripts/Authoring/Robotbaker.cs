using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

public class RobotBaker : Baker<RobotAuthoring>
{
    public override void Bake(RobotAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent<TagRobot>(entity);

        AddComponent(entity, new RobotConfig
        {
            MetersPerSecond = authoring.MetersPerSecond,
            RepathDelay = authoring.RepathDelay
        });

        AddComponent<RobotPosition>(entity);
        AddComponent<RobotDestination>(entity);
        AddComponent<RobotNavigation>(entity);
        AddComponent<RobotIntent>(entity);
        AddBuffer<PathNode>(entity);
        AddComponent<PathRequest>(entity);
        SetComponentEnabled<PathRequest>(entity, false);

        AddComponent<RobotState>(entity);
        AddComponent<ActionTimer>(entity);
        AddComponent<CurrentOrder>(entity);
        AddComponent<RobotAssignment>(entity);

        var material = GetComponent<MeshRenderer>().sharedMaterial;
        DependsOn(material);
        var idleColor = ToLinear(material.GetColor("_BaseColor"));

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

        if (authoring.Package != null)
        {
            AddComponent(entity, new RobotPackage
            {
                Value = GetEntity(authoring.Package, TransformUsageFlags.Dynamic),
                Scale = authoring.Package.transform.localScale.x
            });
        }
    }

    static float4 ToLinear(Color color)
    {
        var linear = color.linear;
        return new float4(linear.r, linear.g, linear.b, linear.a);
    }
}
