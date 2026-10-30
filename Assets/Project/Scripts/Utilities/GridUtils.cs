using Unity.Mathematics;

public static class GridUtils
{
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
