using Unity.Entities;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class WarehouseSimulationGroup : ComponentSystemGroup { }

[UpdateInGroup(typeof(WarehouseSimulationGroup))]
public partial class OrderSystemGroup : ComponentSystemGroup { }

[UpdateInGroup(typeof(WarehouseSimulationGroup))]
[UpdateAfter(typeof(OrderSystemGroup))]
public partial class RobotLogicSystemGroup : ComponentSystemGroup { }

[UpdateInGroup(typeof(WarehouseSimulationGroup))]
[UpdateAfter(typeof(RobotLogicSystemGroup))]
public partial class NavigationSystemGroup : ComponentSystemGroup { }

[UpdateInGroup(typeof(WarehouseSimulationGroup))]
[UpdateAfter(typeof(NavigationSystemGroup))]
public partial class WarehouseVisualSystemGroup : ComponentSystemGroup { }
