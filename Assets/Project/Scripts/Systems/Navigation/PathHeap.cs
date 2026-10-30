using Unity.Collections;

// Binary min-heap of grid indices ordered by A* score. Stale entries are not removed when a cell gets a better
// score: the caller skips them through its closed set, which is cheaper than a decrease-key operation.
public struct PathHeap
{
    struct Node
    {
        public int Index;
        public int Score;
    }

    NativeList<Node> nodes;

    public PathHeap(int capacity, Allocator allocator)
    {
        nodes = new NativeList<Node>(capacity, allocator);
    }

    public bool IsEmpty => nodes.Length == 0;

    public void Push(int index, int score)
    {
        nodes.Add(new Node { Index = index, Score = score });
        int child = nodes.Length - 1;

        while (child > 0)
        {
            int parent = (child - 1) / 2;
            if (nodes[parent].Score <= nodes[child].Score)
                break;

            Swap(parent, child);
            child = parent;
        }
    }

    public int Pop()
    {
        int result = nodes[0].Index;
        int last = nodes.Length - 1;
        nodes[0] = nodes[last];
        nodes.RemoveAt(last);

        int parent = 0;
        while (true)
        {
            int left = parent * 2 + 1;
            int right = left + 1;
            int smallest = parent;

            if (left < nodes.Length && nodes[left].Score < nodes[smallest].Score)
                smallest = left;

            if (right < nodes.Length && nodes[right].Score < nodes[smallest].Score)
                smallest = right;

            if (smallest == parent)
                return result;

            Swap(parent, smallest);
            parent = smallest;
        }
    }

    public void Dispose()
    {
        nodes.Dispose();
    }

    void Swap(int a, int b)
    {
        var swap = nodes[a];
        nodes[a] = nodes[b];
        nodes[b] = swap;
    }
}
