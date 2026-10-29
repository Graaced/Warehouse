using Unity.Entities;

public class PackingStationBaker : Baker<PackingStationAuthoring>
{
    public override void Bake(PackingStationAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent<TagPackingStation>(entity);
        AddComponent<StationPosition>(entity);
    }
}
