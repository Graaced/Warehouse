using Unity.Burst;
using Unity.Mathematics;


[BurstCompile]
public static class GridUtils
{
    /// <summary>
    /// converts a cell (x, y) to a world position.
    /// Assumes 1 unit per cell and a grid on the XZ plane
    /// </summary>
    public static float3 CellToWorld(int2 cell)
    {
        return new float3(cell.x, 0f, cell.y);
    }

    public static int CellIndex(int2 cell, int width)
    {
        return cell.y * width + cell.x;
    }

    public static int2 IndexToCell(int index, int width)
    {
        return new int2(index % width, index / width);
    }

    public static int2 Direction(int index)
    {
        return index switch
        {
            0 => new int2(1, 0),
            1 => new int2(-1, 0),
            2 => new int2(0, 1),
            _ => new int2(0, -1)
        };
    }

    public static int Distance(int2 a, int2 b)
    {
        return math.csum(math.abs(a - b));
    }
}
