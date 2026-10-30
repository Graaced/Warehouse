using Unity.Entities;
using UnityEngine;

public class PackingStationAuthoring : MonoBehaviour
{
    class Baker : Baker<PackingStationAuthoring>
    {
        public override void Bake(PackingStationAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent<StationLayout>(entity);
            AddBuffer<StationSlot>(entity);
        }
    }
}
