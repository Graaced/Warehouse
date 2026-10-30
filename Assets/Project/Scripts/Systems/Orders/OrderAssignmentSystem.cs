using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

[UpdateInGroup(typeof(OrderSystemGroup))]
[UpdateAfter(typeof(OrderSpawnSystem))]
[BurstCompile]
public partial struct OrderAssignmentSystem : ISystem
{
    struct PendingOrder : IComparable<PendingOrder>
    {
        public Entity Order;
        public Entity Shelf;
        public int Sequence;

        public int CompareTo(PendingOrder other)
        {
            return Sequence.CompareTo(other.Sequence);
        }
    }

    struct IdleRobot
    {
        public Entity Robot;
        public int2 Cell;
    }

    [BurstCompile]
    partial struct CollectIdleRobotsJob : IJobEntity
    {
        public NativeList<IdleRobot> Robots;

        public void Execute(Entity entity, in RobotState robotState, in CurrentOrder currentOrder, in RobotPosition position)
        {
            if (robotState.Value == RobotStateKind.Idle && currentOrder.Order == Entity.Null)
                Robots.Add(new IdleRobot { Robot = entity, Cell = position.Cell });
        }
    }

    [BurstCompile]
    partial struct CollectOrdersJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<ShelfStock> StockLookup;
        public NativeList<PendingOrder> Queued;
        public NativeHashSet<Entity> TargetedShelves;

        public void Execute(Entity entity, in OrderState orderState, in OrderShelf orderShelf, in OrderSequence sequence)
        {
            if (StockLookup[orderShelf.Shelf].Empty)
                return;

            // An assigned order whose shelf is still full means a robot is on its way there:
            // sending a second robot would only leave it waiting in front of an empty shelf.
            if (orderState.Value == OrderStateKind.Queued)
                Queued.Add(new PendingOrder { Order = entity, Shelf = orderShelf.Shelf, Sequence = sequence.Value });
            else if (orderState.Value == OrderStateKind.Assigned)
                TargetedShelves.Add(orderShelf.Shelf);
        }
    }

    [BurstCompile]
    struct AssignJob : IJob
    {
        public NativeList<PendingOrder> Queued;
        public NativeList<IdleRobot> Robots;
        public NativeHashSet<Entity> TargetedShelves;

        [ReadOnly] public ComponentLookup<ShelfCell> ShelfLookup;
        public ComponentLookup<OrderState> OrderStateLookup;
        public ComponentLookup<RobotState> RobotStateLookup;
        public ComponentLookup<CurrentOrder> CurrentOrderLookup;
        public ComponentLookup<RobotAssignment> AssignmentLookup;
        public ComponentLookup<RobotDestination> DestinationLookup;
        public ComponentLookup<PathRequest> RequestLookup;

        public void Execute()
        {
            Queued.Sort();

            for (int i = 0; i < Queued.Length && Robots.Length > 0; i++)
            {
                var order = Queued[i];
                if (!TargetedShelves.Add(order.Shelf))
                    continue;

                int2 access = ShelfLookup[order.Shelf].Access;
                int nearest = Nearest(access);
                Assign(Robots[nearest].Robot, order.Order, access);
                Robots.RemoveAtSwapBack(nearest);
            }
        }

        int Nearest(int2 cell)
        {
            int best = 0;
            int bestDistance = int.MaxValue;

            for (int i = 0; i < Robots.Length; i++)
            {
                int distance = GridUtils.Distance(Robots[i].Cell, cell);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = i;
                }
            }

            return best;
        }

        void Assign(Entity robot, Entity order, int2 access)
        {
            OrderStateLookup[order] = new OrderState { Value = OrderStateKind.Assigned };

            var assignment = AssignmentLookup[robot];
            assignment.ParkingSlot = Entity.Null;
            AssignmentLookup[robot] = assignment;

            CurrentOrderLookup[robot] = new CurrentOrder { Order = order };
            RobotStateLookup[robot] = new RobotState { Value = RobotStateKind.ToShelf };
            DestinationLookup[robot] = new RobotDestination { Cell = access };
            RequestLookup.SetComponentEnabled(robot, true);
        }
    }

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<OrderState>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var allocator = state.WorldUpdateAllocator;
        var robots = new NativeList<IdleRobot>(64, allocator);
        var queued = new NativeList<PendingOrder>(256, allocator);
        var targeted = new NativeHashSet<Entity>(256, allocator);

        state.Dependency = new CollectIdleRobotsJob { Robots = robots }.Schedule(state.Dependency);

        state.Dependency = new CollectOrdersJob
        {
            StockLookup = SystemAPI.GetComponentLookup<ShelfStock>(true),
            Queued = queued,
            TargetedShelves = targeted
        }.Schedule(state.Dependency);

        state.Dependency = new AssignJob
        {
            Queued = queued,
            Robots = robots,
            TargetedShelves = targeted,
            ShelfLookup = SystemAPI.GetComponentLookup<ShelfCell>(true),
            OrderStateLookup = SystemAPI.GetComponentLookup<OrderState>(),
            RobotStateLookup = SystemAPI.GetComponentLookup<RobotState>(),
            CurrentOrderLookup = SystemAPI.GetComponentLookup<CurrentOrder>(),
            AssignmentLookup = SystemAPI.GetComponentLookup<RobotAssignment>(),
            DestinationLookup = SystemAPI.GetComponentLookup<RobotDestination>(),
            RequestLookup = SystemAPI.GetComponentLookup<PathRequest>()
        }.Schedule(state.Dependency);
    }
}
