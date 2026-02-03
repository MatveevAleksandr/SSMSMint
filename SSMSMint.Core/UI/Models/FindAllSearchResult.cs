using SSMSMint.Core.UI.Models;
using System.Collections.Generic;

namespace SSMSMint.ResultsGridSearch.Models;

readonly struct FindAllSearchResult(IList<GridCell> gridCells, string searchText, bool matchCase)
{
    public IList<GridCell> GridCells { get; } = gridCells;
    public string SearchText { get; } = searchText;
    public bool MatchCase { get; } = matchCase;
}
