using Unity.Entities;

public class RobotBaker : Baker<RobotAuthoring>
{
    public override void Bake(RobotAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent<TagRobot>(entity);
    }
}
