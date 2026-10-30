using Unity.Entities;
using UnityEngine;

public class LoadingBayAuthoring : MonoBehaviour
{
    class Baker : Baker<LoadingBayAuthoring>
    {
        public override void Bake(LoadingBayAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent<LoadingAreaPosition>(entity);
        }
    }
}
