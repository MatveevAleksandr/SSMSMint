using SSMSMint.Core.Interfaces;
using SSMSMint.Core.Models;

namespace SSMSMint.Core.UI.Models;

public readonly struct GridCell(IGridResultsControlManager gManager, GridCellPoint point, string data, string colHeader, int gridIndex)
{
    public readonly GridCellPoint Point { get; } = point;
    public readonly IGridResultsControlManager GridManager { get; } = gManager;
    public readonly int GridIndex { get; } = gridIndex;
    public readonly string Data { get; } = data;
    public readonly string ColHeader { get; } = colHeader;
}
