using Unity.Entities;

public enum CellKind : byte
{
    Floor,
    Shelf,
    PackingStation,
    Charger,
    LoadingBay,
    Parking
}

public enum CellFill : byte
{
    Full,
    Transparent
}

public struct CellState : IBufferElementData
{
    public CellKind Kind;
    public CellFill Fill;
    public double RespawnTimer;
}
