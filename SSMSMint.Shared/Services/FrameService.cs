using Microsoft.SqlServer.Management.UI.Grid;
using Microsoft.SqlServer.Management.UI.VSIntegration;
using Microsoft.SqlServer.Management.UI.VSIntegration.Editors;
using Microsoft.VisualStudio.Shell.Interop;
using NLog;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SSMSMint.Shared.Services;

/// <summary>
/// Получаем через этот класс необходимые фреймы и контроллы текущего активного фрейма
/// </summary>
public static class FrameService
{
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Получить все гриды с вкладки с Results
    /// </summary>
    public static List<IGridControl> GetAllGridControls()
    {
        try
        {
            _logger.Debug("Getting all grid controls...");
            var allGridControls = new List<IGridControl>();
            var gridResultsPage = GetGridResultPage();

            if (gridResultsPage == null)
            {
                _logger.Warn("Grid results page not found");
                return null;
            }

            var allGridContainers = gridResultsPage.GetType()
                            .GetField("m_gridContainers", BindingFlags.NonPublic | BindingFlags.Instance)
                            .GetValue(gridResultsPage) as CollectionBase;

            if (allGridContainers == null)
            {
                _logger.Warn("Grid containers collection is null");
                return null;
            }

            _logger.Trace($"Found {allGridContainers.Count} grid containers");

            foreach (var container in allGridContainers)
            {
                try
                {
                    var grid = container.GetType()
                                    .GetField("m_grid", BindingFlags.NonPublic | BindingFlags.Instance)
                                    .GetValue(container) as IGridControl;
                    if (grid != null)
                    {
                        allGridControls.Add(grid);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Error while processing grid container");
                }
            }

            _logger.Info($"Successfully retrieved {allGridControls.Count} grid controls");
            return allGridControls;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to get all grid controls");
            return null;
        }
    }

    /// <summary>
    /// Получить последний активный грид с вкладки с Results
    /// </summary>
    private static IGridControl GetLastActiveGridControl()
    {
        try
        {
            _logger.Debug("Getting last active grid control...");
            var gridResultsPage = GetGridResultPage();

            if (gridResultsPage == null)
            {
                _logger.Warn("Grid results page not found");
                return null;
            }

            var lastControl = gridResultsPage.GetType()
                .GetField("lastFocusedControl", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(gridResultsPage) as IGridControl;

            _logger.Info(lastControl != null
                ? "Last active grid control found"
                : "Last active grid control not found");

            return lastControl;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to get last active grid control");
            return null;
        }
    }

    private static object GetGridResultPage()
    {
        try
        {
            _logger.Debug("Getting grid result page...");

            var editorControl = GetSqlScriptEditorControl();

            if (editorControl == null)
            {
                _logger.Warn($"{nameof(editorControl)} is null");
                return null;
            }

            // Получим сам контролл с вкладками Results, Messages и тд
            var sqlResultsControl = editorControl.GetType()
                .GetField("m_sqlResultsControl", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(editorControl);

            if (sqlResultsControl == null)
            {
                _logger.Warn("SQL results control not found in editor");
                return null;
            }

            // Получим вкладку с Results
            var gridResultsPage = sqlResultsControl.GetType()
                .GetField("m_gridResultsPage", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(sqlResultsControl);

            _logger.Info(gridResultsPage != null
                ? "Grid results page found successfully"
                : "Grid results page not found");

            return gridResultsPage;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to get grid result page");
            return null;
        }
    }

    /// <summary>
    /// Получаем текущую активную вкладку
    /// </summary>
    private static IVsWindowFrame GetActiveWindowFrame()
    {
        try
        {
            _logger.Debug("Getting active window frame...");

            var shell = ServiceCache.VsUIShell;
            if (shell == null)
            {
                _logger.Warn("VS UIShell service not available");
                return null;
            }

            shell.GetDocumentWindowEnum(out var enumFrames); // Енумератор всех вкладок
            IVsWindowFrame[] frames = new IVsWindowFrame[1];
            int frameCount = 0;

            while (enumFrames.Next(1, frames, out uint fetched) == 0 && fetched == 1)
            {
                frameCount++;
                var frame = frames[0];
                if (frame == null)
                    continue;

                frame.IsOnScreen(out var onScreenFrame); // Вкладка на которой спозиционирован юзер
                if (onScreenFrame == 1)
                {
                    _logger.Info($"Active window frame found (checked {frameCount} frames)");
                    return frame;
                }
            }

            _logger.Warn($"Active window frame not found (checked {frameCount} frames)");
            return null;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to get active window frame");
            return null;
        }
    }

    /// <summary>
    /// Получаем последний спозиционированный IGridControl. При отсутствии вернет первый
    /// </summary>
    public static IGridControl GetLastFocusedOrFirstGridControl()
    {
        try
        {
            _logger.Debug("Getting last focused or first grid control...");

            var lastActiveGridControl = GetLastActiveGridControl();

            if (lastActiveGridControl != null)
            {
                _logger.Info("Returning last active grid control");
                return lastActiveGridControl;
            }

            var allControls = GetAllGridControls();
            var result = allControls?.FirstOrDefault();

            _logger.Info(result != null
                ? "Returning first grid control from collection"
                : "No grid controls available");

            return result;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to get last focused or first grid control");
            return null;
        }
    }

    public static IGridControl GetNextGrid(IGridControl grid)
    {
        try
        {
            _logger.Debug("Getting next grid control...");

            if (grid == null)
            {
                _logger.Warn("Input grid is null");
                return null;
            }

            var allGridControls = GetAllGridControls();

            if (allGridControls == null)
            {
                _logger.Warn("Grid controls collection is null");
                return null;
            }

            var index = allGridControls.IndexOf(grid);

            if (index == -1)
            {
                _logger.Warn("Specified grid not found in collection");
                return null;
            }
            else if (index + 1 == allGridControls.Count)
            {
                _logger.Info("Take first grid");
                return allGridControls.FirstOrDefault();
            }
            else
            {
                _logger.Info("Take next grid");
                return allGridControls[index + 1];
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to get next grid control");
            return null;
        }
    }

    public static IGridControl GetPrevGrid(IGridControl grid)
    {
        try
        {
            _logger.Debug("Getting previous grid control...");

            if (grid == null)
            {
                _logger.Warn("Input grid is null");
                return null;
            }

            var allGridControls = GetAllGridControls();

            if (allGridControls == null)
            {
                _logger.Warn("Grid controls collection is null");
                return null;
            }

            var index = allGridControls.IndexOf(grid);

            if (index == -1)
            {
                _logger.Warn("Specified grid not found in collection");
                return null;
            }
            else if (index == 0)
            {
                _logger.Info("Take last grid");
                return allGridControls.LastOrDefault();
            }
            else
            {
                _logger.Info("Take prev grid");
                return allGridControls[index - 1];
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to get previous grid control");
            return null;
        }
    }

    /// <summary>
    /// Получить активное окно редактора
    /// </summary>
    public static SqlScriptEditorControl GetSqlScriptEditorControl()
    {
        _logger.Debug("Getting sql script editor control...");

        var frame = GetActiveWindowFrame();

        if (frame == null)
        {
            _logger.Warn("Active window frame not found");
            return null;
        }

        frame.GetProperty((int)__VSFPROPID.VSFPROPID_DocView, out var docViewObj);

        if (docViewObj == null)
        {
            _logger.Warn("Current active frame content not found");
            return null;
        }

        if (!(docViewObj is SqlScriptEditorControl editorControl))
        {
            _logger.Warn($"Current active frame content is not {nameof(SqlScriptEditorControl)}. Actual type: {docViewObj.GetType().Name}");
            return null;
        }
        return editorControl;
    }
}
