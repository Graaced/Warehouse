using Unity.Entities;
using Unity.Mathematics;

public enum CellKind : byte
{
    Floor,
    Shelf,
    PackingStation,
    PackingService,
    Charger,
    LoadingBay,
    Parking,
    ParkingLane
}

public enum CellLane : byte
{
    None,
    Top,
    CenterWest,
    CenterEast,
    Bottom
}

// The static part of the warehouse never changes after generation, so it lives in a blob:
// read-only, shared by every job without dependencies, and cheap to access.
public struct GridMapBlob
{
    public int Width;
    public int Height;
    public BlobArray<CellKind> Kinds;
    public BlobArray<CellLane> Lanes;

    public bool Contains(int2 cell)
    {
        return cell.x >= 0 && cell.y >= 0 && cell.x < Width && cell.y < Height;
    }

    public int Index(int2 cell)
    {
        return cell.y * Width + cell.x;
    }

    public CellKind Kind(int2 cell)
    {
        return Kinds[Index(cell)];
    }

    public CellLane Lane(int2 cell)
    {
        return Lanes[Index(cell)];
    }
}

public struct GridMap : IComponentData
{
    public BlobAssetReference<GridMapBlob> Value;
}
