using Unity.Burst;
using Unity.Entities;

// Publishes, before anyone moves, the cell each robot wants next and its priority. MovementJob reads the
// intents of other robots to detect head-on conflicts without touching their navigation state.
[BurstCompile]
[WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)]
public partial struct IntentJob : IJobEntity
{
    public void Execute(ref RobotIntent intent, in RobotNavigation navigation, in RobotState robotState,
        in DynamicBuffer<PathNode> path, EnabledRefRO<PathRequest> request)
    {
        intent.HasNext = !navigation.Moving && !request.ValueRO && navigation.PathIndex < path.Length;
        intent.Next = intent.HasNext ? path[navigation.PathIndex].Cell : default;
        intent.Priority = robotState.IsCarrying ? RobotIntent.CarrierPriority : RobotIntent.EmptyPriority;
    }
}
