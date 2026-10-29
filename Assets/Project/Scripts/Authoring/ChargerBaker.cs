using Unity.Entities;

public class ChargerBaker : Baker<ChargerAuthoring>
{
    public override void Bake(ChargerAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent<TagCharger>(entity);
        AddComponent<ChargerPosition>(entity);
        AddComponent<ChargerState>(entity);
    }
}
