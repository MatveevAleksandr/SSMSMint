using SSMSMint.Shared.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SqlServer.Management.UI.Grid;
using System;
using SSMSMint.Shared.Extentions;

namespace SSMSMint.ResultsGridSearch
{
    internal class GridPosition
    {
        public IGridControl GridControl { get; set; }
        public long RowIndex { get; set; }
        public int ColIndex { get; set; }
        public string CellData { get; set; }

        public string DisplayColHeader { get; set; }
        public string DisplayGridNumber { get; set; }
        public string DisplayRowNumber { get; set; }

        public GridPosition(IGridControl gridControl, long rowIndex, int colIndex)
        {
            GridControl = gridControl ?? throw new ArgumentNullException(nameof(gridControl));

            var activeFrameService = ServicesLocator.ServiceProvider.GetRequiredService<FrameService>();
            var schemaTable = gridControl.GridStorage.GetSchemaTable();

            RowIndex = rowIndex;
            ColIndex = colIndex;
            CellData = gridControl.GridStorage.GetCellDataAsString(rowIndex, colIndex);
            DisplayGridNumber = $"Grid {activeFrameService.GetAllGridControls().IndexOf(gridControl) + 1}";
            DisplayRowNumber = (rowIndex + 1).ToString();
            DisplayColHeader = schemaTable.Rows[colIndex - 1][0]?.ToString();
        }

        public override bool Equals(object obj)
        {
            if (obj is GridPosition other)
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

        public GridPosition Clone() => new GridPosition(GridControl, RowIndex, ColIndex);
    }
}
