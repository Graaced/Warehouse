using Unity.Entities;
using Unity.Mathematics;

public struct RobotPalette : IComponentData
{
    public float4 Idle;
    public float4 ToShelf;
    public float4 PickingUp;
    public float4 Transporting;
    public float4 Unloading;
    public float4 Loading;
    public float4 ToCharger;
    public float4 Charging;

    public float4 Get(RobotStateKind state)
    {
        return state switch
        {
            RobotStateKind.ToShelf => ToShelf,
            RobotStateKind.PickingUp => PickingUp,
            RobotStateKind.Transporting => Transporting,
            RobotStateKind.Unloading => Unloading,
            RobotStateKind.Loading => Loading,
            RobotStateKind.ToCharger => ToCharger,
            RobotStateKind.Charging => Charging,
            _ => Idle
        };
    }
}
