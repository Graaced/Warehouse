using Unity.Entities;

public enum CellKind : byte
{
    Floor,
    Shelf,
    PackingStation,
    Charger,
    LoadingBay,
    Parking,
    PackingService,
    ParkingLane
}

public enum CellFill : byte
{
    Full,
    Transparent
}

public enum CellLane : byte
{
    None,
    Top,
    CenterWest,
    CenterEast,
    Bottom
}

public struct CellState : IBufferElementData
{
    public CellKind Kind;
    public CellFill Fill;
    public CellLane Lane;
    public double RespawnTimer;
}
