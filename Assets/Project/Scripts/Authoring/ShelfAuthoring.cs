using Unity.Entities;
using UnityEngine;

public class ShelfAuthoring : MonoBehaviour
{
    public GameObject Package;

    class Baker : Baker<ShelfAuthoring>
    {
        public override void Bake(ShelfAuthoring authoring)
        {
            if (authoring.Package == null)
            {
                Debug.LogError("[ShelfAuthoring] The shelf needs a Package child!", authoring);
                return;
            }

            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent<ShelfCell>(entity);
            AddComponent<ShelfStock>(entity);
            AddComponent<ShelfVisual>(entity);

            AddComponent(entity, new ShelfPackage
            {
                Value = GetEntity(authoring.Package, TransformUsageFlags.Dynamic),
                Scale = authoring.Package.transform.localScale.x
            });
        }
    }
}
