using Unity.Burst;
using Unity.Entities;

[BurstCompile]
[WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)]
public partial struct IntentJob : IJobEntity
{
    public void Execute(ref RobotIntent intent, in RobotNavigation navigation, in RobotState robotState,
        in DynamicBuffer<PathNode> path, EnabledRefRO<PathRequest> request)
    {
        intent.HasNext = !navigation.Moving && !request.ValueRO && navigation.PathIndex < path.Length;
        intent.Next = intent.HasNext ? path[navigation.PathIndex].Cell : default;
        intent.Priority = robotState.IsCarrying ? 1 : 0;
    }
}
