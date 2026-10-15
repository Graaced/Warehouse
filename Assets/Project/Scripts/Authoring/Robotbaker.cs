using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

public class RobotBaker : Baker<RobotAuthoring>
{
    public override void Bake(RobotAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent<TagRobot>(entity);

        AddComponent(entity, new RobotMovement
        {
            CurrentCell = new Unity.Mathematics.int2(0, 0),
            TargetCell = new Unity.Mathematics.int2(10, 5),
            MoveSpeed = 2f
        });

        AddComponent(entity, new URPMaterialPropertyBaseColor
        {
            Value = new float4(0.2f, 0.7f, 1f, 1f) // azure
        });

        AddComponent(entity, new URPMaterialPropertyEmissionColor
        {
            Value = new float4(0f, 0.5f, 1f, 1f) // azure glow
        });
    }
}
