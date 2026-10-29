using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(PathfindingSystem))]
[BurstCompile]
public partial struct RandomDestinationSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<TagGrid>();
        state.RequireForUpdate<TagRobot>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        state.CompleteDependency();

        var grid = SystemAPI.GetSingletonEntity<TagGrid>();
        var width = SystemAPI.GetComponent<GridDimensions>(grid).Width;
        var cells = SystemAPI.GetBuffer<CellState>(grid);
        var random = Random.CreateFromIndex((uint)(SystemAPI.Time.ElapsedTime * 1000));

        foreach (var (navigation, position, destination, path, entity) in SystemAPI
                     .Query<RefRO<RobotNavigation>, RefRO<RobotPosition>, RefRW<RobotDestination>, DynamicBuffer<PathNode>>()
                     .WithEntityAccess())
        {
            bool idle = !navigation.ValueRO.Moving && navigation.ValueRO.PathIndex >= path.Length &&
                        position.ValueRO.Cell.Equals(destination.ValueRO.Cell);

            if (!idle || SystemAPI.IsComponentEnabled<PathRequest>(entity))
                continue;

            for (int attempt = 0; attempt < 20; attempt++)
            {
                int index = random.NextInt(cells.Length);
                if (cells[index].Kind != CellKind.Floor)
                    continue;

                destination.ValueRW.Cell = GridUtils.IndexToCell(index, width);
                SystemAPI.SetComponentEnabled<PathRequest>(entity, true);
                break;
            }
        }
    }
}
