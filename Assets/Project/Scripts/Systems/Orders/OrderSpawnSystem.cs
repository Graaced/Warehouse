using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

[UpdateInGroup(typeof(OrderSystemGroup))]
[BurstCompile]
public partial struct OrderSpawnSystem : ISystem
{
    double accumulator;
    int nextSequence;
    Random random;
    EntityQuery ordersQuery;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<ConfigWarehouse>();
        state.RequireForUpdate<GridShelf>();
        state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        ordersQuery = SystemAPI.QueryBuilder().WithAll<OrderState>().Build();
        random = Random.CreateFromIndex(7);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var config = SystemAPI.GetSingleton<ConfigWarehouse>();
        accumulator += SystemAPI.Time.DeltaTime * config.OrdersPerMinute / 60.0;

        if (accumulator < 1.0)
            return;

        // Orders that can't be stored are dropped instead of piling up: an unbounded queue would make
        // the assignment cost grow forever when robots can't keep up with the order rate.
        int room = config.MaxPendingOrders - ordersQuery.CalculateEntityCount();
        var shelves = SystemAPI.GetSingletonBuffer<GridShelf>(true);
        var commands = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        for (; accumulator >= 1.0; accumulator -= 1.0)
        {
            if (room-- <= 0)
                continue;

            var order = commands.CreateEntity();
            commands.AddComponent(order, new OrderState { Value = OrderStateKind.Queued });
            commands.AddComponent(order, new OrderShelf { Shelf = shelves[random.NextInt(shelves.Length)].Shelf });
            commands.AddComponent(order, new OrderSequence { Value = nextSequence++ });
        }
    }
}
