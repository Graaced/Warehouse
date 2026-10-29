using Unity.Burst;
using Unity.Entities;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(OrderAssignmentSystem))]
[BurstCompile]
public partial struct RespawnSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<TagGrid>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        state.CompleteDependency();

        double now = SystemAPI.Time.ElapsedTime;
        var cells = SystemAPI.GetSingletonBuffer<CellState>();

        for (int i = 0; i < cells.Length; i++)
        {
            var cell = cells[i];
            if (cell.Fill != CellFill.Transparent || now < cell.RespawnTimer)
                continue;

            cell.Fill = CellFill.Full;
            cells[i] = cell;
        }
    }
}
