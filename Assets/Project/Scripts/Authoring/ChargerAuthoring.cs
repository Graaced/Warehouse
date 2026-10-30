using Unity.Entities;
using UnityEngine;

public class ChargerAuthoring : MonoBehaviour
{
    class Baker : Baker<ChargerAuthoring>
    {
        public override void Bake(ChargerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent<StationLayout>(entity);
            AddBuffer<StationSlot>(entity);
        }
    }
}
