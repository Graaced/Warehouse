using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(RobotStateMachineSystem))]
[BurstCompile]
public partial struct OrderAssignmentSystem : ISystem
{
    struct QueuedOrder : IComparable<QueuedOrder>
    {
        public Entity Entity;
        public int Sequence;
        public int2 Shelf;

        public int CompareTo(QueuedOrder other)
        {
            return Sequence.CompareTo(other.Sequence);
        }
    }

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<TagGrid>();
        state.RequireForUpdate<TagOrder>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        state.CompleteDependency();

        var grid = SystemAPI.GetSingletonEntity<TagGrid>();
        int width = SystemAPI.GetComponent<GridDimensions>(grid).Width;
        var cells = SystemAPI.GetBuffer<CellState>(grid);

        var robots = new NativeList<Entity>(Allocator.Temp);
        foreach (var (robotState, currentOrder, entity) in SystemAPI
                     .Query<RefRO<RobotState>, RefRO<CurrentOrder>>()
                     .WithEntityAccess())
        {
            if (robotState.ValueRO.Value == RobotStateKind.Idle && currentOrder.ValueRO.Order == Entity.Null)
                robots.Add(entity);
        }

        var orders = new NativeList<QueuedOrder>(Allocator.Temp);
        foreach (var (orderState, destination, sequence, entity) in SystemAPI
                     .Query<RefRO<OrderState>, RefRO<DestinationCell>, RefRO<OrderSequence>>()
                     .WithEntityAccess())
        {
            var cell = cells[GridUtils.CellIndex(destination.ValueRO.Cell, width)];
            if (orderState.ValueRO.Value == OrderStateKind.Queued && cell.Fill == CellFill.Full)
                orders.Add(new QueuedOrder { Entity = entity, Sequence = sequence.ValueRO.Value, Shelf = destination.ValueRO.Cell });
        }

        orders.Sort();

        for (int i = 0; i < orders.Length && robots.Length > 0; i++)
        {
            int2 serviceCell = ServiceCell(orders[i].Shelf, cells, width);
            int nearest = Nearest(ref state, robots, serviceCell);
            Assign(ref state, robots[nearest], orders[i].Entity, serviceCell);
            robots.RemoveAtSwapBack(nearest);
        }

        robots.Dispose();
        orders.Dispose();
    }

    int Nearest(ref SystemState state, NativeList<Entity> robots, int2 cell)
    {
        int best = 0;
        int bestDistance = int.MaxValue;

        for (int i = 0; i < robots.Length; i++)
        {
            int distance = GridUtils.Distance(SystemAPI.GetComponent<RobotPosition>(robots[i]).Cell, cell);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = i;
            }
        }

        return best;
    }

    void Assign(ref SystemState state, Entity robot, Entity order, int2 serviceCell)
    {
        SystemAPI.SetComponent(order, new OrderState { Value = OrderStateKind.Assigned });
        SystemAPI.SetComponent(order, new OrderAssignee { Robot = robot });

        var assignment = SystemAPI.GetComponent<RobotAssignment>(robot);
        if (assignment.ParkingSlot != Entity.Null)
        {
            var slot = SystemAPI.GetComponent<ParkingSlot>(assignment.ParkingSlot);
            slot.Robot = Entity.Null;
            SystemAPI.SetComponent(assignment.ParkingSlot, slot);
            assignment.ParkingSlot = Entity.Null;
            SystemAPI.SetComponent(robot, assignment);
        }

        SystemAPI.SetComponent(robot, new CurrentOrder { Order = order });
        SystemAPI.SetComponent(robot, new RobotState { Value = RobotStateKind.ToShelf });
        SystemAPI.SetComponent(robot, new RobotDestination { Cell = serviceCell });
        SystemAPI.SetComponentEnabled<PathRequest>(robot, true);
    }

    static int2 ServiceCell(int2 shelf, DynamicBuffer<CellState> cells, int width)
    {
        for (int d = 0; d < 4; d++)
        {
            int2 cell = shelf + GridUtils.Direction(d);
            if (cells[GridUtils.CellIndex(cell, width)].Kind == CellKind.Floor)
                return cell;
        }

        return shelf;
    }
}
