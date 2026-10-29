using Unity.Entities;

public class LoadingBayBaker : Baker<LoadingBayAuthoring>
{
    public override void Bake(LoadingBayAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent<TagLoadingArea>(entity);
        AddComponent<LoadingAreaPosition>(entity);
    }
}
