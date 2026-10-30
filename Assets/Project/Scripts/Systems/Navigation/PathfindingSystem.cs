using Unity.Burst;
using Unity.Entities;

[UpdateInGroup(typeof(NavigationSystemGroup))]
[BurstCompile]
public partial struct PathfindingSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<ConfigWarehouse>();
        state.RequireForUpdate<GridMap>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var grid = SystemAPI.GetSingletonEntity<GridMap>();

        state.Dependency = new PathfindingJob
        {
            Map = SystemAPI.GetComponent<GridMap>(grid).Value,
            OccupantLookup = SystemAPI.GetBufferLookup<CellOccupant>(true),
            Grid = grid,
            LanePenalty = SystemAPI.GetSingleton<ConfigWarehouse>().LanePenalty,
            Seed = (int)(SystemAPI.Time.ElapsedTime * 10)
        }.ScheduleParallel(state.Dependency);
    }
}
