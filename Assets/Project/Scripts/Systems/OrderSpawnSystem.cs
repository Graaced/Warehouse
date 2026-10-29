using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(OrderAssignmentSystem))]
[BurstCompile]
public partial struct OrderSpawnSystem : ISystem
{
    double accumulator;
    int nextSequence;
    Random random;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<ConfigWarehouse>();
        state.RequireForUpdate<TagGrid>();
        random = Random.CreateFromIndex(7);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var config = SystemAPI.GetSingleton<ConfigWarehouse>();
        var grid = SystemAPI.GetSingletonEntity<TagGrid>();
        int width = SystemAPI.GetComponent<GridDimensions>(grid).Width;
        var cells = SystemAPI.GetBuffer<CellState>(grid);

        accumulator += SystemAPI.Time.DeltaTime * config.OrdersPerMinute / 60.0;
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        while (accumulator >= 1.0)
        {
            accumulator -= 1.0;

            int index = RandomShelfIndex(cells);
            if (index < 0)
                break;

            var order = ecb.CreateEntity();
            ecb.AddComponent<TagOrder>(order);
            ecb.AddComponent(order, new OrderState { Value = OrderStateKind.Queued });
            ecb.AddComponent(order, new DestinationCell { Cell = GridUtils.IndexToCell(index, width) });
            ecb.AddComponent<OrderAssignee>(order);
            ecb.AddComponent(order, new OrderSequence { Value = nextSequence++ });
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    int RandomShelfIndex(DynamicBuffer<CellState> cells)
    {
        for (int attempt = 0; attempt < 100; attempt++)
        {
            int index = random.NextInt(cells.Length);
            if (cells[index].Kind == CellKind.Shelf)
                return index;
        }

        return -1;
    }
}
