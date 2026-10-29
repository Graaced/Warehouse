using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(PathfindingSystem))]
[BurstCompile]
public partial struct RobotStateMachineSystem : ISystem
{
    Random random;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<ConfigWarehouse>();
        state.RequireForUpdate<TagGrid>();
        state.RequireForUpdate<TagRobot>();
        random = Random.CreateFromIndex(11);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        state.CompleteDependency();

        var config = SystemAPI.GetSingleton<ConfigWarehouse>();
        var grid = SystemAPI.GetSingletonEntity<TagGrid>();
        int width = SystemAPI.GetComponent<GridDimensions>(grid).Width;
        var cells = SystemAPI.GetBuffer<CellState>(grid);
        double now = SystemAPI.Time.ElapsedTime;

        var freeSlots = new NativeList<Entity>(Allocator.Temp);
        foreach (var (slot, entity) in SystemAPI.Query<RefRO<ParkingSlot>>().WithEntityAccess())
        {
            if (slot.ValueRO.Active && slot.ValueRO.Robot == Entity.Null)
                freeSlots.Add(entity);
        }

        var ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (robotState, timer, currentOrder, entity) in SystemAPI
                     .Query<RefRW<RobotState>, RefRW<ActionTimer>, RefRW<CurrentOrder>>()
                     .WithEntityAccess())
        {
            switch (robotState.ValueRO.Value)
            {
                case RobotStateKind.Idle:
                    if (SystemAPI.GetComponent<RobotAssignment>(entity).ParkingSlot == Entity.Null)
                        ReserveParking(ref state, entity, freeSlots);
                    break;

                case RobotStateKind.ToShelf:
                    if (HasArrived(ref state, entity))
                    {
                        robotState.ValueRW.Value = RobotStateKind.PickingUp;
                        timer.ValueRW.Expiry = now + config.LiftSeconds;
                    }
                    break;

                case RobotStateKind.PickingUp:
                    var shelf = SystemAPI.GetComponent<DestinationCell>(currentOrder.ValueRO.Order).Cell;
                    int index = GridUtils.CellIndex(shelf, width);

                    if (now < timer.ValueRO.Expiry || cells[index].Fill != CellFill.Full)
                        break;

                    var cell = cells[index];
                    cell.Fill = CellFill.Transparent;
                    cell.RespawnTimer = now + random.NextFloat(config.RespawnMinSeconds, config.RespawnMaxSeconds);
                    cells[index] = cell;

                    ecb.DestroyEntity(currentOrder.ValueRO.Order);
                    currentOrder.ValueRW.Order = Entity.Null;
                    robotState.ValueRW.Value = RobotStateKind.Idle;
                    break;
            }
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
        freeSlots.Dispose();
    }

    bool HasArrived(ref SystemState state, Entity robot)
    {
        var navigation = SystemAPI.GetComponent<RobotNavigation>(robot);
        var position = SystemAPI.GetComponent<RobotPosition>(robot);
        var destination = SystemAPI.GetComponent<RobotDestination>(robot);

        return !navigation.Moving && !SystemAPI.IsComponentEnabled<PathRequest>(robot) && position.Cell.Equals(destination.Cell);
    }

    void GoTo(ref SystemState state, Entity robot, int2 cell)
    {
        SystemAPI.SetComponent(robot, new RobotDestination { Cell = cell });
        SystemAPI.SetComponentEnabled<PathRequest>(robot, true);
    }

    void ReserveParking(ref SystemState state, Entity robot, NativeList<Entity> freeSlots)
    {
        if (freeSlots.Length == 0)
            return;

        int2 position = SystemAPI.GetComponent<RobotPosition>(robot).Cell;
        int best = 0;
        int bestDistance = int.MaxValue;

        for (int i = 0; i < freeSlots.Length; i++)
        {
            int distance = GridUtils.Distance(SystemAPI.GetComponent<ParkingSlot>(freeSlots[i]).Cell, position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = i;
            }
        }

        var slotEntity = freeSlots[best];
        freeSlots.RemoveAtSwapBack(best);

        var slot = SystemAPI.GetComponent<ParkingSlot>(slotEntity);
        slot.Robot = robot;
        SystemAPI.SetComponent(slotEntity, slot);
        SystemAPI.SetComponent(robot, new RobotAssignment { ParkingSlot = slotEntity });
        GoTo(ref state, robot, slot.Cell);
    }
}
