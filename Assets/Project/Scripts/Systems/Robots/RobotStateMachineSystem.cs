using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

[UpdateInGroup(typeof(RobotLogicSystemGroup))]
[BurstCompile]
public partial struct RobotStateMachineSystem : ISystem
{
    [BurstCompile]
    partial struct RebuildReservationsJob : IJobEntity
    {
        public ReservationTable Table;

        public void Execute(Entity entity, in RobotAssignment assignment)
        {
            if (assignment.ParkingSlot != Entity.Null && Table.ParkingIndex.TryGetValue(assignment.ParkingSlot, out int parking))
                Table.ParkingTaken[parking] = true;

            if (assignment.Station == Entity.Null || !Table.StationIndex.TryGetValue(assignment.Station, out int station))
                return;

            Table.StationLoad[station]++;

            if (assignment.Stage == StationStage.Approaching)
                return;

            Table.Occupy(station, assignment.Slot, entity);

            if (assignment.Stage == StationStage.InQueue)
                Table.MarkWaiting(station, assignment.Slot, assignment.QueuedAt);
        }
    }

    EntityQuery stationQuery;
    EntityQuery parkingQuery;
    uint frame;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<ConfigWarehouse>();
        state.RequireForUpdate<GridMap>();
        state.RequireForUpdate<RobotState>();
        state.RequireForUpdate<WarehouseStats>();
        state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();

        stationQuery = SystemAPI.QueryBuilder().WithAll<StationLayout, StationSlot>().Build();
        parkingQuery = SystemAPI.QueryBuilder().WithAll<ParkingSlot>().Build();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var config = SystemAPI.GetSingleton<ConfigWarehouse>();
        var grid = SystemAPI.GetSingletonEntity<GridMap>();
        var allocator = state.WorldUpdateAllocator;

        var stations = stationQuery.ToEntityArray(allocator);
        var parkingEntities = parkingQuery.ToEntityArray(allocator);
        var table = ReservationTable.Create(stations, parkingEntities, config.StationLength * 2, allocator);

        state.Dependency = new RebuildReservationsJob { Table = table }.Schedule(state.Dependency);

        state.Dependency = new RobotStateMachineJob
        {
            Table = table,
            Stations = stations,
            StationLayouts = stationQuery.ToComponentDataArray<StationLayout>(allocator),
            ParkingEntities = parkingEntities,
            ParkingSlots = parkingQuery.ToComponentDataArray<ParkingSlot>(allocator),
            SlotLookup = SystemAPI.GetBufferLookup<StationSlot>(true),
            OccupantLookup = SystemAPI.GetBufferLookup<CellOccupant>(true),
            OrderShelfLookup = SystemAPI.GetComponentLookup<OrderShelf>(true),
            OrderStateLookup = SystemAPI.GetComponentLookup<OrderState>(),
            StockLookup = SystemAPI.GetComponentLookup<ShelfStock>(),
            StatsLookup = SystemAPI.GetComponentLookup<WarehouseStats>(),
            Commands = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged),
            Map = SystemAPI.GetComponent<GridMap>(grid).Value,
            Grid = grid,
            Stats = SystemAPI.GetSingletonEntity<WarehouseStats>(),
            Config = config,
            Now = SystemAPI.Time.ElapsedTime,
            DeltaTime = SystemAPI.Time.DeltaTime,
            Random = Random.CreateFromIndex(frame++)
        }.Schedule(state.Dependency);
    }
}
