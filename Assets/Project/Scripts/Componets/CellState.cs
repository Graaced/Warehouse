using Unity.Entities;

public struct CellState : IBufferElementData
{
    public byte Type;        // 0 = Full, 1 = Transparent
    public double RespawnTimer;
}
