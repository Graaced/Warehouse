using Unity.Entities;
using UnityEngine;

public class ParkingSlotAuthoring : MonoBehaviour
{
    class Baker : Baker<ParkingSlotAuthoring>
    {
        public override void Bake(ParkingSlotAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent<ParkingSlot>(entity);
        }
    }
}
