using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public partial struct MovementJob : IJobEntity
{
    public float DeltaTime;

    public void Execute(ref LocalTransform transform, ref RobotMovement movement)
    {
        float3 currentPos = transform.Position;
        float3 targetPos = GridUtils.CellToWorld(movement.CurrentCell);
        targetPos.y = currentPos.y;

        // if it has reached the center of the current cell...
        if (math.distancesq(currentPos, targetPos) < 0.0001f)
        {
            // ...and has not yet reached the final destination, it moves to the next cell
            if (!GridUtils.IsAtTarget(movement.CurrentCell, movement.TargetCell))
            {
                movement.CurrentCell = GridUtils.NextCell(movement.CurrentCell, movement.TargetCell);
                targetPos = GridUtils.CellToWorld(movement.CurrentCell);
                targetPos.y = currentPos.y;
            }
        }

        // move to the current cell (which is now the next one to reach)
        float3 delta = targetPos - currentPos;
        float distance = math.length(delta);

        if (distance > 0.0001f)
        {
            float step = math.min(distance, movement.MoveSpeed * DeltaTime);
            transform.Position = currentPos + math.normalizesafe(delta) * step;
        }
    }
}
