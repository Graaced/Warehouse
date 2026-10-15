using Unity.Burst;
using Unity.Entities;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[BurstCompile]
public partial struct MovementSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        // the system works only if there are robots with RobotMovement
        state.RequireForUpdate<RobotMovement>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float dt = SystemAPI.Time.DeltaTime;

        state.Dependency = new MovementJob
        {
            DeltaTime = dt
        }.ScheduleParallel(state.Dependency);
    }
}
