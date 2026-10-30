using Unity.Mathematics;

// Single source of truth for the warehouse geometry. The generation system places entities from these
// coordinates and bakes them into data (station slots, approach cells, shelf access cells), so no runtime
// system needs to know the shape of the warehouse.
//
// Rows from the bottom: margin, parking, bottom lane, bottom shelf block, center lane, top shelf block, top lane.
// Columns from the left: margin, packing counters, side aisle, shelf pairs, side aisle, bays and chargers, margin.
public struct WarehouseLayout
{
    public const int ServiceOffset = 1;
    public const int QueueOffset = 4;
    public const int ApproachOffset = 5;
    public const int MinSideAisleWidth = ApproachOffset;

    public int Width;
    public int Height;
    public int ShelvesX;
    public int Span;
    public int ParkingHeight;
    public int BottomBlockZ;
    public int TopBlockZ;
    public int CenterLaneZ;
    public int CenterSplitZ;
    public int TopLaneZ;
    public int RightColumnX;
    public int RightColumnCount;
    public int StationHalfLength;

    ConfigWarehouse config;

    public static WarehouseLayout Create(in ConfigWarehouse config)
    {
        var layout = new WarehouseLayout { config = config };

        layout.Span = config.ShelfColumnPairs * 2 + (config.ShelfColumnPairs - 1) * config.AisleWidth;

        // Parking rows come in pairs around a driving lane (slot, lane, slot, slot, lane, slot...),
        // so every slot touches a lane or the bottom margin and no parked robot can be boxed in.
        int slotRows = (config.MaxRobots + layout.Span - 1) / layout.Span;
        layout.ParkingHeight = slotRows + slotRows / 2;

        layout.Width = layout.Span + config.SideAisleWidth * 2 + 4;
        layout.Height = layout.ParkingHeight + config.BottomBlockRows + config.TopBlockRows + config.AisleWidth * 3 + 1;

        layout.ShelvesX = config.SideAisleWidth + 2;
        layout.BottomBlockZ = layout.ParkingHeight + config.AisleWidth + 1;
        layout.CenterLaneZ = layout.BottomBlockZ + config.BottomBlockRows;
        layout.CenterSplitZ = layout.CenterLaneZ + config.AisleWidth / 2;
        layout.TopBlockZ = layout.CenterLaneZ + config.AisleWidth;
        layout.TopLaneZ = layout.TopBlockZ + config.TopBlockRows;

        layout.RightColumnX = layout.Width - 2;
        layout.RightColumnCount = config.LoadingBayCount + config.ChargerCount;
        layout.StationHalfLength = config.StationLength / 2;
        return layout;
    }

    public bool Fits
    {
        get
        {
            int room = Height - 2;
            int footprint = config.StationLength + 1;
            return config.PackingStationCount * footprint <= room && RightColumnCount * footprint <= room;
        }
    }

    public int ShelfCount => config.ShelfColumnPairs * 2 * (config.BottomBlockRows + config.TopBlockRows);

    public void Shelf(int index, out int2 cell, out int2 access)
    {
        int rowsPerColumn = config.BottomBlockRows + config.TopBlockRows;
        int column = index / rowsPerColumn;
        int row = index % rowsPerColumn;
        int pair = column / 2;
        bool rightOfPair = column % 2 == 1;

        int x = ShelvesX + pair * (2 + config.AisleWidth) + (rightOfPair ? 1 : 0);
        int z = row < config.BottomBlockRows ? BottomBlockZ + row : TopBlockZ + row - config.BottomBlockRows;

        cell = new int2(x, z);
        access = cell + new int2(rightOfPair ? 1 : -1, 0);
    }

    public int2 PackingStation(int index)
    {
        return new int2(1, Distribute(index, config.PackingStationCount));
    }

    // Packing counters stand on the left edge and open towards +x, loading bays and chargers stand on the right
    // edge and open towards -x: their service, queue and approach cells mirror each other.
    public static int Facing(StationKind kind)
    {
        return kind == StationKind.Packing ? 1 : -1;
    }

    public int2 ServiceCell(int2 station, StationKind kind, int offset)
    {
        return station + new int2(ServiceOffset * Facing(kind), offset);
    }

    public int2 QueueCell(int2 station, StationKind kind, int offset)
    {
        return station + new int2(QueueOffset * Facing(kind), offset);
    }

    public int2 ApproachCell(int2 station, StationKind kind)
    {
        return station + new int2(ApproachOffset * Facing(kind), 0);
    }

    public int2 RightColumn(int index)
    {
        return new int2(RightColumnX, Distribute(index, RightColumnCount));
    }

    public bool IsLoadingBay(int rightColumnIndex)
    {
        return rightColumnIndex < config.LoadingBayCount;
    }

    public int2 ParkingCell(int index)
    {
        int row = index / Span;
        return new int2(ShelvesX + index % Span, ParkingHeight - row - (row + 1) / 2);
    }

    public bool IsParkingArea(int2 cell)
    {
        return cell.y >= 1 && cell.y <= ParkingHeight && cell.x >= ShelvesX && cell.x < ShelvesX + Span;
    }

    public CellLane Lane(int2 cell)
    {
        if (cell.x < ShelvesX || cell.x >= ShelvesX + Span)
            return CellLane.None;

        if (cell.y > ParkingHeight && cell.y < BottomBlockZ)
            return CellLane.Bottom;

        if (cell.y >= CenterLaneZ && cell.y < CenterSplitZ)
            return CellLane.CenterEast;

        if (cell.y >= CenterSplitZ && cell.y < TopBlockZ)
            return CellLane.CenterWest;

        return cell.y >= TopLaneZ ? CellLane.Top : CellLane.None;
    }

    int Distribute(int index, int count)
    {
        int length = Height - 2;
        return 1 + (index * 2 + 1) * length / (count * 2);
    }
}
