using SSMSMint.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SSMSMint.Core.Interfaces;

public interface IGridResultsControlManager
{
    public string GetColumnHeader(int index);
    public IReadOnlyList<int> GetSelectedColumnIndices();

    public void GetGridSize(out long rowCnt, out int colCnt);

    public Task FocusCellAsync(GridCellPoint gcPoint);
    public string GetCellData(GridCellPoint gcPoint);
    public GridCellPoint GetCurrentPosition();
}
