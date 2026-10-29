using Unity.Entities;

public class ShelfBaker : Baker<ShelfAuthoring>
{
    public override void Bake(ShelfAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent<TagShelf>(entity);

        if (authoring.Package != null)
            AddComponent(entity, new ShelfPackage { Value = GetEntity(authoring.Package, TransformUsageFlags.Dynamic) });
    }
}
