using Unity.Burst;
using Unity.Entities;

[UpdateInGroup(typeof(OrderSystemGroup))]
[UpdateBefore(typeof(OrderAssignmentSystem))]
[BurstCompile]
public partial struct ShelfRespawnSystem : ISystem
{
    [BurstCompile]
    partial struct RespawnJob : IJobEntity
    {
        public double Now;

        public void Execute(ref ShelfStock stock)
        {
            if (stock.Empty && Now >= stock.RespawnAt)
                stock.Empty = false;
        }
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        state.Dependency = new RespawnJob { Now = SystemAPI.Time.ElapsedTime }.ScheduleParallel(state.Dependency);
    }
}
