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

    /// <summary>
    /// returns the next cell to traverse to move from 'current' to 'target'.
    /// strategy: first align X, then align Y.
    /// </summary>
    public static int2 NextCell(int2 current, int2 target)
    {
        int2 delta = target - current;

        if (delta.x != 0)
            return current + new int2(math.sign(delta.x), 0);

        if (delta.y != 0)
            return current + new int2(0, math.sign(delta.y));

        return current; // already arrived
    }

    /// <summary>
    /// True if the robot has reached the target cell.
    /// </summary>
    public static bool IsAtTarget(int2 current, int2 target)
    {
        return math.all(current == target);
    }
}
