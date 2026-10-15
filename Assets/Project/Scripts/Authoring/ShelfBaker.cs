using Unity.Entities;
using Unity.Transforms;

public class ShelfBaker : Baker<ShelfAuthoring>
{
    public override void Bake(ShelfAuthoring authoring)
    {
        var Entity = GetEntity(TransformUsageFlags.Dynamic);

        AddComponent<TagShelf>(Entity);
    }
}
