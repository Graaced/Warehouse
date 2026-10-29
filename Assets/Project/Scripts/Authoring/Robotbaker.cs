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

        AddComponent(entity, new RobotMovement
        {
            CurrentCell = int2.zero,
            TargetCell = int2.zero,
            MoveSpeed = 4f
        });

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
