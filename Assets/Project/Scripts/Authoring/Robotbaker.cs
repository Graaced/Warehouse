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

        var material = GetComponent<MeshRenderer>().sharedMaterial;
        DependsOn(material);
        var color = material.GetColor("_BaseColor").linear;

        AddComponent(entity, new URPMaterialPropertyBaseColor
        {
            Value = new float4(color.r, color.g, color.b, color.a)
        });

        if (authoring.Package != null)
            AddComponent(entity, new RobotPackage { Value = GetEntity(authoring.Package, TransformUsageFlags.Dynamic) });
    }
}
