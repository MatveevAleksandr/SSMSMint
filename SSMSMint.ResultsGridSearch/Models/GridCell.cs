using Microsoft.SqlServer.Management.UI.Grid;
using System;

namespace SSMSMint.ResultsGridSearch.Models;

public class GridCell(IGridControl gridControl, long rowIndex, int colIndex, string colHeader, int gridIndex, string data)
{
    public IGridControl GridControl { get; set; } = gridControl ?? throw new ArgumentNullException(nameof(gridControl));
    public long RowIndex { get; set; } = rowIndex;
    public int ColIndex { get; set; } = colIndex;
    public string Data { get; set; } = data;
    public string ColHeader { get; set; } = colHeader;
    public int GridIndex { get; set; } = gridIndex;

    public override bool Equals(object obj)
    {
        if (obj is GridCell other)
        {
            return this.GridControl == other.GridControl &&
                   this.RowIndex == other.RowIndex &&
                   this.ColIndex == other.ColIndex;
        }
        return false;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 23 + (GridControl?.GetHashCode() ?? 0);
            hash = hash * 23 + RowIndex.GetHashCode();
            hash = hash * 23 + ColIndex.GetHashCode();
            return hash;
        }
    }

    public GridCell Clone() => new(GridControl, RowIndex, ColIndex, ColHeader, GridIndex, Data);
}