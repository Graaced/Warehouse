using Unity.Entities;
using UnityEngine;

public class ConfigWarehouseBaker : Baker<ConfigWarehouseAuthoring>
{
    public override void Bake(ConfigWarehouseAuthoring authoring)
    {
        // VALIDATION

        if (authoring.RobotCount <= 0)
        {
            Debug.LogError("[ConfigWarehouse] RobotCount has to be > 0 !", authoring);
            return;
        }

        if (authoring.ShelfCount <= 0)
        {
            Debug.LogError("[ConfigWarehouse] ShelfCount has to be > 0!", authoring);
            return;
        }

        if (authoring.GridWidth < 2 || authoring.GridHeight < 2)
        {
            Debug.LogError("[ConfigWarehouse] The grid must be at least 2x2!", authoring);
            return;
        }

        if (authoring.Robot == null)
        {
            Debug.LogError("[ConfigWarehouse] Robot unassigned!!", authoring);
            return;
        }

        if (authoring.Shelf == null)
        {
            Debug.LogError("[ConfigWarehouse] Shelf unassigned!!", authoring);
            return;
        }

        if (authoring.PackingStation == null)
        {
            Debug.LogError("[ConfigWarehouse] PackingStation unassigned!!", authoring);
            return;
        }


        //ENTITY CREATION

        var entity = GetEntity(TransformUsageFlags.None);

        AddComponent(entity, new ConfigWarehouse
            {

                RobotCount = authoring.RobotCount,
                ShelfCount = authoring.ShelfCount,
                GridWidth = authoring.GridWidth,
                GridHeight = authoring.GridHeight,
                Robot = GetEntity(authoring.Robot, TransformUsageFlags.Dynamic),
                Shelf = GetEntity(authoring.Shelf, TransformUsageFlags.Dynamic),
                PackingStation = GetEntity(authoring.PackingStation, TransformUsageFlags.Dynamic),
        });



    }
}
