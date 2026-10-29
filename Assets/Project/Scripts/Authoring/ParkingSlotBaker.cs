using Unity.Entities;

public class ParkingSlotBaker : Baker<ParkingSlotAuthoring>
{
    public override void Bake(ParkingSlotAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent<TagParkingSlot>(entity);
        AddComponent<ParkingSlot>(entity);
    }
}
