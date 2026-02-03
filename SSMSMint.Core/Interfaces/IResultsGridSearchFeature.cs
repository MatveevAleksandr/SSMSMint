using SSMSMint.Core.UI.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SSMSMint.Core.Interfaces;

public interface IResultsGridSearchFeature
{
    public IReadOnlyList<GridCell> FindAll(string searchText, bool matchCase, bool matchWholeCell, GridLookInTypeEn gridLookIn);
    public Task<bool> TryFindNextAsync(string searchText, bool matchCase, bool matchWholeCell, GridLookInTypeEn gridLookIn);
    public Task<bool> TryFindPrevAsync(string searchText, bool matchCase, bool matchWholeCell, GridLookInTypeEn gridLookIn);
}
