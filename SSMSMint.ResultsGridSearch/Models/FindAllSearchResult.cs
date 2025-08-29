using System.Collections.Generic;

namespace SSMSMint.ResultsGridSearch.Models;

class FindAllSearchResult(IEnumerable<GridCell> gridCells, string searchText, bool matchCase)
{
    public IEnumerable<GridCell> GridCells { get; set; } = gridCells;
    public string SearchText { get; set; } = searchText;
    public bool MatchCase { get; set; } = matchCase;
}
