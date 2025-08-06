﻿using SSMSMint.Shared.Extentions;
using SSMSMint.Shared.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SqlServer.Management.UI.Grid;
using Microsoft.VisualStudio.Shell;
using NLog;
using System;
using System.Collections.Generic;

namespace SSMSMint.ResultsGridSearch
{
    internal class SearchProcessor
    {
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public List<GridPosition> FindAll(string searchText, bool matchCase, GridLookIn gridLookIn)
        {
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                var frameService = ServicesLocator.ServiceProvider.GetRequiredService<FrameService>();
                var gridsToSearch = new List<IGridControl>();
                var result = new List<GridPosition>();

                if (gridLookIn == GridLookIn.CurrentGrid)
                {
                    var currentGridControl = frameService.GetLastFocusedOrFirstGridControl();

                    if (currentGridControl != null)
                    {
                        gridsToSearch.Add(currentGridControl);
                    }
                    else
                    {
                        _logger.Warn("No grid controls found for searching");
                    }
                }
                else if (gridLookIn == GridLookIn.AllGrids)
                {
                    var allGridControls = frameService.GetAllGridControls();

                    if (allGridControls != null)
                    {
                        gridsToSearch.AddRange(allGridControls);
                    }
                    else
                    {
                        _logger.Warn("No grid controls found for searching");
                    }
                }

                foreach (var gridControl in gridsToSearch)
                {
                    var gridStorage = gridControl.GridStorage;
                    var schemaTable = gridStorage.GetSchemaTable();

                    for (long r = 0; r < gridStorage.NumRows(); r++)
                    {
                        for (int c = 1; c <= schemaTable.Rows.Count; c++)
                        {
                            var cellData = gridStorage.GetCellDataAsString(r, c);

                            if (cellData == "NULL")
                            {
                                continue;
                            }

                            var matchCaseOption = matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

                            if (cellData.IndexOf(searchText, matchCaseOption) >= 0)
                            {
                                result.Add(new GridPosition(gridControl, r, c));
                            }
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to complete search");
                throw;
            }
        }

        public bool TryFindNext(string searchText, bool matchCase, GridLookIn gridLookIn)
        {
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                var activeFrameService = ServicesLocator.ServiceProvider.GetRequiredService<FrameService>();
                var currentGridControl = activeFrameService.GetLastFocusedOrFirstGridControl();

                if (currentGridControl == null)
                {
                    _logger.Info("Нет грида для поиска");
                    return false;
                }

                currentGridControl.GetCurrentCell(out var currentGridControlRowIndex, out var currentGridControlColIndex);

                var startPosition = new GridPosition(currentGridControl, currentGridControlRowIndex, currentGridControlColIndex);

                // Если не выбрано никакой ячейки (хотя это тяжело воспроизводимо) - выставим первую
                if (startPosition.RowIndex == -1 || startPosition.ColIndex == -1)
                {
                    startPosition.ColIndex = 1;
                    startPosition.RowIndex = 0;
                }

                var currentFindPosition = startPosition.Clone();

                do
                {
                    var gridStorage = currentFindPosition.GridControl.GridStorage;
                    var schemaTable = gridStorage.GetSchemaTable();

                    // Если мы стоим на последней ячейке в гриде - начнем сначала с учетом того смотрим ли мы внутри текущего грида или всех
                    if (currentFindPosition.ColIndex == schemaTable.Rows.Count && currentFindPosition.RowIndex == gridStorage.NumRows() - 1)
                    {
                        if (gridLookIn == GridLookIn.AllGrids)
                        {
                            currentFindPosition.GridControl = activeFrameService.GetNextGrid(currentFindPosition.GridControl);
                            if (currentFindPosition.GridControl == null)
                            {
                                throw new ArgumentNullException($"Ошибка поиска {nameof(FrameService.GetNextGrid)}. Грид в котором ищем не найден в текущем окне.");
                            }
                        }
                        currentFindPosition.ColIndex = 1;
                        currentFindPosition.RowIndex = 0;
                    }
                    // Если мы стоим на последнем столбце - надо сделать переход на следующую строку
                    else if (currentFindPosition.ColIndex == schemaTable.Rows.Count)
                    {
                        currentFindPosition.ColIndex = 1;
                        currentFindPosition.RowIndex += 1;
                    }
                    // Тут просто смещаемся вправо по той же строке
                    else
                    {
                        currentFindPosition.ColIndex += 1;
                    }

                    // Анализируем уже новую ячейку
                    var cellData = currentFindPosition.GridControl.GridStorage.GetCellDataAsString(currentFindPosition.RowIndex, currentFindPosition.ColIndex);

                    if (cellData == "NULL")
                    {
                        continue;
                    }

                    var matchCaseOption = matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

                    if (cellData.IndexOf(searchText, matchCaseOption) >= 0)
                    {
                        FocusCell(currentFindPosition);
                        return true;
                    }
                }
                while (!currentFindPosition.Equals(startPosition));

                return false;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to find next match");
                throw;
            }
        }

        public bool TryFindPrev(string searchText, bool matchCase, GridLookIn gridLookIn)
        {
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                var activeFrameService = ServicesLocator.ServiceProvider.GetRequiredService<FrameService>();
                var currentGridControl = activeFrameService.GetLastFocusedOrFirstGridControl();

                if (currentGridControl == null)
                {
                    _logger.Info("Нет грида для поиска");
                    return false;
                }

                currentGridControl.GetCurrentCell(out var currentGridControlRowIndex, out var currentGridControlColIndex);

                var startPosition = new GridPosition(currentGridControl, currentGridControlRowIndex, currentGridControlColIndex);

                // Если не выбрано никакой ячейки (хотя это тяжело воспроизводимо) - выставим последнюю
                if (startPosition.RowIndex == -1 || startPosition.ColIndex == -1)
                {
                    var gridStorage = startPosition.GridControl.GridStorage;
                    var schemaTable = gridStorage.GetSchemaTable();
                    startPosition.ColIndex = schemaTable.Rows.Count;
                    startPosition.RowIndex = gridStorage.NumRows() - 1;
                }

                var currentFindPosition = startPosition.Clone();

                do
                {
                    // Если мы стоим на первой ячейке в гриде - начнем с конца с учетом того смотрим ли мы внутри текущего грида или всех
                    if (currentFindPosition.ColIndex == 1 && currentFindPosition.RowIndex == 0)
                    {
                        if (gridLookIn == GridLookIn.AllGrids)
                        {
                            currentFindPosition.GridControl = activeFrameService.GetPrevGrid(currentFindPosition.GridControl);
                            if (currentFindPosition.GridControl == null)
                            {
                                throw new ArgumentNullException($"Ошибка поиска {nameof(FrameService.GetPrevGrid)}. Грид в котором ищем не найден в текущем окне.");
                            }
                        }
                        // Смотрим уже на предыдущий грид или тот же самый
                        var gridStorage = currentFindPosition.GridControl.GridStorage;
                        var schemaTable = gridStorage.GetSchemaTable();
                        currentFindPosition.ColIndex = schemaTable.Rows.Count;
                        currentFindPosition.RowIndex = gridStorage.NumRows() - 1;
                    }
                    // Если мы стоим на первом столбце - надо сделать переход на предыдущую строку
                    else if (currentFindPosition.ColIndex == 1)
                    {
                        var gridStorage = currentFindPosition.GridControl.GridStorage;
                        var schemaTable = gridStorage.GetSchemaTable();
                        currentFindPosition.ColIndex = schemaTable.Rows.Count;
                        currentFindPosition.RowIndex -= 1;
                    }
                    // Тут просто смещаемся влево по той же строке
                    else
                    {
                        currentFindPosition.ColIndex -= 1;
                    }

                    // Анализируем уже новую ячейку
                    var cellData = currentFindPosition.GridControl.GridStorage.GetCellDataAsString(currentFindPosition.RowIndex, currentFindPosition.ColIndex);

                    if (cellData == "NULL")
                    {
                        continue;
                    }

                    var matchCaseOption = matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

                    if (cellData.IndexOf(searchText, matchCaseOption) >= 0)
                    {
                        FocusCell(currentFindPosition);
                        return true;
                    }
                }
                while (!currentFindPosition.Equals(startPosition));

                return false;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to find previous match");
                throw;
            }
        }

        public void FocusCell(GridPosition position)
        {
            try
            {
                var selectionManager = position.GridControl.GetSelectionManager();

                selectionManager.Clear(); // Очистим все прочие selection
                selectionManager.StartNewBlock(position.RowIndex, position.ColIndex); // Выделим найденную ячейку
                position.GridControl.EnsureCellIsVisible(position.RowIndex, position.ColIndex); // Подвинем скролл чтоб было видно выделенную ячейку
                (position.GridControl as GridControl).Focus();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Failed to focus cell at position: {position}");
                throw;
            }
        }
    }
}
