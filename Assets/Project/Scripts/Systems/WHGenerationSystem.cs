using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public partial struct WHGenerationSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<ConfigWarehouse>();
    }


    public void OnUpdate(ref SystemState state)
    {
        var config = SystemAPI.GetSingleton<ConfigWarehouse>();
        var em = state.EntityManager;

        // creation of the Grid entity with the cell buffer
        var gridEntity = em.CreateEntity();
        em.AddComponentData<GridDimensions > (gridEntity, new GridDimensions
        {
            Width = config.GridWidth,
            Height = config.GridHeight
        });

        var cellsBuffer = em.AddBuffer<CellState>(gridEntity);
        for (int i = 0; i < config.GridWidth * config.GridHeight; i++)
        {
            cellsBuffer.Add(new CellState { Type = 0, RespawnTimer = 0 });
        }


        // deploy the robots in the waiting area
        var robotInstances = em.Instantiate(config.Robot, config.RobotCount, Allocator.Temp);
        for (int i = 0; i < robotInstances.Length; i++)
        {
            // start Cell 
            int2 startCell = new int2(
                i % config.GridWidth,
                (i / config.GridWidth) % config.GridHeight
            );

            // arrival cell: the corner opposite the starting point.
            int2 targetCell = new int2(
                config.GridWidth - 1 - startCell.x,
                config.GridHeight - 1 - startCell.y
            );

            // 3d position aligned with the starting cell.
            var pos = new LocalTransform
            {
                Position = new float3(startCell.x, 0f, startCell.y),
                Rotation = quaternion.identity,
                Scale = 1f
            };
            em.SetComponentData(robotInstances[i], pos);

            // set robot movement
            em.SetComponentData(robotInstances[i], new RobotMovement
            {
                CurrentCell = startCell,
                TargetCell = targetCell,
                MoveSpeed = 2f
            });

           
        }

        robotInstances.Dispose();


        // place the shelves at random positions on the grid
        var rng = Random.CreateFromIndex(42);
        for (int i = 0; i < config.ShelfCount; i++)
        {
            int x = rng.NextInt(1, config.GridWidth - 1);
            int z = rng.NextInt(1, config.GridHeight - 1);

            var scaffale = em.Instantiate(config.Shelf);
            em.SetComponentData(scaffale, new LocalTransform
            {
                Position = new float3(x, 0, z),
                Rotation = quaternion.identity,
                Scale = 1f
            });

            int cellIndex = z * config.GridWidth + x;
            cellsBuffer[cellIndex] = new CellState { Type = 0, RespawnTimer = 0 };
        }

        //  packaging station
        var stazione = em.Instantiate(config.PackingStation);
        em.SetComponentData(stazione, new LocalTransform
        {
            Position = new float3(config.GridWidth - 1, 0, config.GridHeight - 1),
            Rotation = quaternion.identity,
            Scale = 1f
        });

        // 5. Distruggi il singleton ConfigMagazzino (non serve più)
        var configEntity = SystemAPI.GetSingletonEntity<ConfigWarehouse>();
        em.DestroyEntity(configEntity);

    }
}
