using SSMSMint.Core.Interfaces;
using SSMSMint.Core.Models;
using SSMSMint.Core.UI.Models;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SSMSMint.Features;

public class ResultsGridSearchFeature(IWorkspaceManager wManager) : IResultsGridSearchFeature
{
    private readonly Logger logger = LogManager.GetCurrentClassLogger();
    private enum SearchDirection { Forward, Backward }
    private readonly struct SearchPosition(IGridResultsControlManager grid, GridCellPoint point)
    {
        public readonly IGridResultsControlManager GridControl = grid;
        public readonly GridCellPoint Point = point;
    }


    public IReadOnlyList<GridCell> FindAll(string searchText, bool matchCase, bool matchWholeCell, GridLookInTypeEn gridLookIn)
    {
        try
        {
            var gridsToSearch = new List<IGridResultsControlManager>();
            var result = new List<GridCell>();
            var allgcManagers = wManager.GetAllGridControls() ?? throw new Exception("No grid controls found for searching");

            if (allgcManagers == null || allgcManagers.Count == 0)
                throw new Exception("No grid controls found for searching");

            if (gridLookIn == GridLookInTypeEn.CurrentGrid)
            {
                var currentGC = wManager.GetLastActiveGridControl() ?? allgcManagers.First() ?? throw new Exception("No grid controls found for searching");
                gridsToSearch.Add(currentGC);
            }
            else if (gridLookIn == GridLookInTypeEn.AllGrids)
            {
                gridsToSearch.AddRange(allgcManagers);
            }

            foreach (var gcManager in gridsToSearch)
            {
                gcManager.GetGridSize(out var rowCnt, out var colCnt);

                for (long r = 0; r < rowCnt; r++)
                {
                    for (int c = 1; c <= colCnt; c++)
                    {
                        var p = new GridCellPoint(r, c);
                        var cellData = gcManager.GetCellData(p);
                        var colHeader = gcManager.GetColumnHeader(c);

                        if (cellData == "NULL")
                        {
                            continue;
                        }

                        if (IsMatch(cellData, searchText, matchCase, matchWholeCell))
                        {
                            var cHeader = gcManager.GetColumnHeader(c);
                            var gIndex = allgcManagers.IndexOf(gcManager);
                            result.Add(new GridCell(gcManager, p, cellData, cHeader, gIndex));
                        }
                    }
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to complete search");
            throw;
        }
    }

    public async Task<bool> TryFindNextAsync(string searchText, bool matchCase, bool matchWholeCell, GridLookInTypeEn gridLookIn)
    {
        try
        {
            return await TryFindAsync(searchText, matchCase, matchWholeCell, gridLookIn, SearchDirection.Forward);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to find next");
            throw;
        }
    }

    public async Task<bool> TryFindPrevAsync(string searchText, bool matchCase, bool matchWholeCell, GridLookInTypeEn gridLookIn)
    {
        try
        {
            return await TryFindAsync(searchText, matchCase, matchWholeCell, gridLookIn, SearchDirection.Backward);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to find previous");
            throw;
        }
    }




    private async Task<bool> TryFindAsync(string searchText, bool matchCase, bool matchWholeCell, GridLookInTypeEn gridLookIn, SearchDirection direction)
    {
        var allGrids = wManager.GetAllGridControls();

        if (allGrids == null || allGrids.Count == 0)
            throw new Exception("No grid controls found for searching");

        // Определяем начальную позицию       
        var startPosition = GetStartSearchPosition(allGrids.First());

        bool searchInAllGrids = gridLookIn == GridLookInTypeEn.AllGrids;

        IEnumerable<SearchPosition> positionsToSearch;
        if (direction == SearchDirection.Forward)
        {
            positionsToSearch = EnumerateSearchPositionsForward(allGrids, startPosition, searchInAllGrids);
        }
        else
        {
            positionsToSearch = EnumerateSearchPositionsBackward(allGrids, startPosition, searchInAllGrids);
        }

        // Skip(1), чтобы не проверять текущую ячейку, а начать со следующей
        foreach (var position in positionsToSearch.Skip(1))
        {
            // Если мы вернулись к началу - совпадений нет
            if (position.Equals(startPosition))
            {
                return false;
            }

            string cellData = position.GridControl.GetCellData(position.Point);

            if (cellData == "NULL")
            {
                continue;
            }

            if (IsMatch(cellData, searchText, matchCase, matchWholeCell))
            {
                await position.GridControl.FocusCellAsync(position.Point);
                return true;
            }
        }

        return false;
    }

    private bool IsMatch(string cellData, string searchText, bool matchCase, bool matchWholeCell)
    {
        var comparisonType = matchCase ? StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase;

        if (matchWholeCell)
            return string.Equals(cellData, searchText, comparisonType);
        else
            return cellData.IndexOf(searchText, comparisonType) >= 0;
    }

    /// <summary>
    /// Итератор, который перечисляет все возможные позиции поиска вперед, начиная с указанной.
    /// </summary>
    private IEnumerable<SearchPosition> EnumerateSearchPositionsForward(
        IList<IGridResultsControlManager> allGrids,
        SearchPosition startPosition,
        bool searchInAllGrids)
    {
        var currentPosition = startPosition;

        while (true)
        {
            // Возвращаем текущую позицию, чтобы ее можно было проверить
            yield return currentPosition;

            var grid = currentPosition.GridControl;
            grid.GetGridSize(out long rowCount, out int colCount);

            long nextRow = currentPosition.Point.Row;
            int nextCol = currentPosition.Point.Column + 1; // Смещаемся вправо

            // Проверяем выход за пределы строки
            if (nextCol > colCount)
            {
                nextCol = 1; // Переходим в начало следующей строки
                nextRow++;
            }

            // Проверяем выход за пределы грида
            if (nextRow >= rowCount)
            {
                if (!searchInAllGrids)
                {
                    // Если ищем только в текущем гриде, начинаем с его начала
                    currentPosition = new SearchPosition(grid, new GridCellPoint(0, 1));
                    continue;
                }

                // Иначе переходим к следующему гриду
                int nextGridIndex = (allGrids.IndexOf(currentPosition.GridControl) + 1) % allGrids.Count;
                currentPosition = new SearchPosition(allGrids[nextGridIndex], new GridCellPoint(0, 1));
                continue;
            }

            // Если все в порядке, просто обновляем позицию
            currentPosition = new SearchPosition(grid, new GridCellPoint(nextRow, nextCol));
        }
    }

    /// <summary>
    /// Итератор, который перечисляет все возможные позиции поиска назад, начиная с указанной.
    /// </summary>
    private IEnumerable<SearchPosition> EnumerateSearchPositionsBackward(
        IList<IGridResultsControlManager> allGrids,
        SearchPosition startPosition,
        bool searchInAllGrids)
    {
        var currentPosition = startPosition;

        while (true)
        {
            // Возвращаем текущую позицию, чтобы ее можно было проверить
            yield return currentPosition;

            var grid = currentPosition.GridControl;
            grid.GetGridSize(out long rowCount, out int colCount);

            long nextRow = currentPosition.Point.Row;
            int nextCol = currentPosition.Point.Column - 1; // Смещаемся влево

            // Проверяем выход за пределы строки
            if (nextCol < 1)
            {
                nextCol = colCount; // Переходим в конец предыдущей строки
                nextRow--;
            }

            // Проверяем выход за пределы грида
            if (nextRow < 0)
            {
                if (!searchInAllGrids)
                {
                    // Если ищем только в текущем гриде, то переходим в его самый конец
                    currentPosition = new SearchPosition(grid, new GridCellPoint(rowCount - 1, colCount));
                    continue;
                }

                // Иначе переходим к предыдущему гриду
                int nextGridIndex = allGrids.IndexOf(currentPosition.GridControl) - 1;
                if (nextGridIndex < 0)
                {
                    nextGridIndex = allGrids.Count - 1; // Зацикливаемся на последний грид в списке
                }
                var nextGrid = allGrids[nextGridIndex];
                nextGrid.GetGridSize(out var nextGridRowCnt, out var nextGridColCnt);
                currentPosition = new SearchPosition(nextGrid, new GridCellPoint(nextGridRowCnt - 1, nextGridColCnt));
                continue;
            }

            // Если все в порядке, просто обновляем позицию
            currentPosition = new SearchPosition(grid, new GridCellPoint(nextRow, nextCol));
        }
    }

    private SearchPosition GetStartSearchPosition(IGridResultsControlManager defaultGrid)
    {
        var activeGrid = wManager.GetLastActiveGridControl() ?? defaultGrid ?? throw new Exception("No grid controls found for searching");
        var currentCell = activeGrid.GetCurrentPosition();
        // Если не выбрано никакой ячейки (хотя это тяжело воспроизводимо) - выставим первую
        long startRow = (currentCell.Row < 0) ? 0 : currentCell.Row;
        int startCol = (currentCell.Column < 0) ? 1 : currentCell.Column;

        return new SearchPosition(activeGrid, new GridCellPoint(startRow, startCol));
    }
}
