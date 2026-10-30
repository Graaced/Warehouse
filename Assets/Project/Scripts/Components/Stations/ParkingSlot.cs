using Unity.Entities;
using Unity.Mathematics;

public struct ParkingSlot : IComponentData
{
    public int2 Cell;
    public int Index;
    public bool Active;
}
