using Unity.Entities;
using Unity.Mathematics;

public struct StationSlot : IBufferElementData
{
    public int2 Cell;
    public bool Service;
}
