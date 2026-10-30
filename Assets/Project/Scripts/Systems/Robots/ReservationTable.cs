using Unity.Collections;
using Unity.Entities;

// Frame-local view of who holds which parking slot and station slot. It is rebuilt from RobotAssignment at the
// start of every frame (RobotAssignment is the only persistent owner) and then kept up to date by the state
// machine job as robots take and release slots during that same frame.
public struct ReservationTable
{
    public NativeHashMap<Entity, int> StationIndex;
    public NativeArray<int> StationLoad;
    public NativeArray<Entity> SlotRobot;
    public NativeArray<bool> SlotWaiting;
    public NativeArray<double> SlotQueuedAt;
    public int SlotsPerStation;

    public NativeHashMap<Entity, int> ParkingIndex;
    public NativeArray<bool> ParkingTaken;

    public static ReservationTable Create(NativeArray<Entity> stations, NativeArray<Entity> parkingSlots, int slotsPerStation,
        AllocatorManager.AllocatorHandle allocator)
    {
        var table = new ReservationTable
        {
            StationIndex = new NativeHashMap<Entity, int>(stations.Length, allocator),
            StationLoad = CollectionHelper.CreateNativeArray<int>(stations.Length, allocator),
            SlotRobot = CollectionHelper.CreateNativeArray<Entity>(stations.Length * slotsPerStation, allocator),
            SlotWaiting = CollectionHelper.CreateNativeArray<bool>(stations.Length * slotsPerStation, allocator),
            SlotQueuedAt = CollectionHelper.CreateNativeArray<double>(stations.Length * slotsPerStation, allocator),
            SlotsPerStation = slotsPerStation,
            ParkingIndex = new NativeHashMap<Entity, int>(parkingSlots.Length, allocator),
            ParkingTaken = CollectionHelper.CreateNativeArray<bool>(parkingSlots.Length, allocator)
        };

        for (int i = 0; i < stations.Length; i++)
            table.StationIndex[stations[i]] = i;

        for (int i = 0; i < parkingSlots.Length; i++)
            table.ParkingIndex[parkingSlots[i]] = i;

        return table;
    }

    public int SlotKey(int station, int slot)
    {
        return station * SlotsPerStation + slot;
    }

    public void Occupy(int station, int slot, Entity robot)
    {
        int key = SlotKey(station, slot);
        SlotRobot[key] = robot;
        SlotWaiting[key] = false;
        SlotQueuedAt[key] = 0;
    }

    public void Release(int station, int slot)
    {
        Occupy(station, slot, Entity.Null);
    }

    public void MarkWaiting(int station, int slot, double since)
    {
        int key = SlotKey(station, slot);
        SlotWaiting[key] = true;
        SlotQueuedAt[key] = since;
    }
}
