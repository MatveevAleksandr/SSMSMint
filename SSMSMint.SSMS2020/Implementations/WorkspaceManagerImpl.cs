using EnvDTE;
using EnvDTE80;
using SSMSMint.Core.Interfaces;
using SSMSMint.Core.Models;
using SSMSMint.Core.UI.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.UI.Grid;
using Microsoft.SqlServer.Management.UI.VSIntegration;
using Microsoft.SqlServer.Management.UI.VSIntegration.Editors;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using NLog;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace SSMSMint.SSMS2020.Implementations;

internal class WorkspaceManagerImpl(DTE2 dte, AsyncPackage package) : IWorkspaceManager
{
    private readonly Logger logger = LogManager.GetCurrentClassLogger();

    public ITextDocumentManager CreateNewSqlBlankScript()
    {
        ServiceCache.ScriptFactory.CreateNewBlankScript(ScriptType.Sql);
        var doc = (TextDocument)dte.ActiveDocument.Object("TextDocument") ?? throw new Exception("ActiveDocument not found");
        return new TextDocumentManagerImpl(doc);
    }

    public async Task<ITextDocumentManager> CreateNewFileAsync(string type, string name)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        dte.ItemOperations.NewFile(type, name);
        var doc = (TextDocument)dte.ActiveDocument.Object("TextDocument") ?? throw new Exception("ActiveDocument not found");
        return new TextDocumentManagerImpl(doc);
    }

    public SqlConnectionMetaData GetActiveEditorConnection()
    {
        var control = GetSqlScriptEditorControl();
        var conn = (SqlConnection)control.GetType().GetField("m_connection", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(control);
        return new SqlConnectionMetaData(conn.DataSource, conn.Database);
    }

    /// <summary>
    /// Получить последний активный грид с вкладки с Results
    /// </summary>
    public IGridResultsControlManager GetLastActiveGridControl()
    {
        try
        {
            logger.Debug("Getting last active grid control...");
            var gridResultsPage = GetGridResultPage();

            if (gridResultsPage == null)
            {
                logger.Error("Grid results page not found");
                throw new Exception("Grid results page not found");
            }

            var lastControl = gridResultsPage.GetType()
                .GetField("lastFocusedControl", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(gridResultsPage) as IGridControl;

            if (lastControl == null)
            {
                logger.Info("Last active grid control not found");
                return null;
            }

            logger.Info("Last active grid control found");
            return new GridResultsControlManagerImpl(lastControl);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to get last active grid control");
            throw new Exception("Failed to get last active grid control", ex);
        }
    }

    /// <summary>
    /// Получить все гриды с вкладки с Results
    /// </summary>
    public IList<IGridResultsControlManager> GetAllGridControls()
    {
        try
        {
            logger.Debug("Getting all grid controls...");
            var allGridControls = new List<IGridResultsControlManager>();
            var gridResultsPage = GetGridResultPage();

            if (gridResultsPage == null)
            {
                logger.Warn("Grid results page not found");
                return null;
            }

            var allGridContainers = gridResultsPage.GetType()
                            .GetField("m_gridContainers", BindingFlags.NonPublic | BindingFlags.Instance)
                            .GetValue(gridResultsPage) as CollectionBase;

            if (allGridContainers == null)
            {
                logger.Warn("Grid containers collection is null");
                return null;
            }

            logger.Trace($"Found {allGridContainers.Count} grid containers");

            foreach (var container in allGridContainers)
            {
                try
                {
                    var grid = container.GetType()
                                    .GetField("m_grid", BindingFlags.NonPublic | BindingFlags.Instance)
                                    .GetValue(container) as IGridControl;
                    if (grid != null)
                    {
                        allGridControls.Add(new GridResultsControlManagerImpl(grid));
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Error while processing grid container");
                }
            }

            logger.Info($"Successfully retrieved {allGridControls.Count} grid controls");
            return allGridControls;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to get all grid controls");
            return null;
        }
    }

    public async Task ShowToolWindowAsync<T>(IToolWindowParams twParams = null) where T : IToolWindowCore
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        ToolWindowPane toolWindow = package.FindToolWindow(typeof(T), 0, true);
        if ((null == toolWindow) || (null == toolWindow.Frame))
        {
            logger.Error($"{nameof(T)} Cannot create tool window");
            throw new Exception($"{nameof(T)} Cannot create tool window");
        }
        var toolWindowFrame = (IVsWindowFrame)toolWindow.Frame;
        Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(toolWindowFrame.Show());

        if (twParams != null)
            (toolWindow as IToolWindowCore).Initialize(twParams);
    }

    public async Task CloseToolWindowAsync<T>() where T : IToolWindowCore
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        ToolWindowPane toolWindow = package.FindToolWindow(typeof(T), 0, true);
        if ((null == toolWindow) || (null == toolWindow.Frame))
        {
            logger.Error($"{nameof(T)} Cannot create tool window");
            throw new Exception($"{nameof(T)} Cannot create tool window");
        }
        var toolWindowFrame = (IVsWindowFrame)toolWindow.Frame;
        toolWindowFrame.CloseFrame((uint)__FRAMECLOSE.FRAMECLOSE_NoSave);
    }





    /// <summary>
    /// Получить активное окно редактора
    /// </summary>
    private SqlScriptEditorControl GetSqlScriptEditorControl()
    {
        logger.Debug("Getting sql script editor control...");

        var frame = GetOnScreenWindowFrame();

        if (frame == null)
        {
            logger.Error("Active window frame not found");
            throw new Exception("Active window frame not found");
        }

        frame.GetProperty((int)__VSFPROPID.VSFPROPID_DocView, out var docViewObj);

        if (docViewObj == null)
        {
            logger.Error("Current active frame content not found");
            throw new Exception("Current active frame content not found");
        }

        if (!(docViewObj is SqlScriptEditorControl editorControl))
        {
            logger.Error($"Current active frame content is not {nameof(SqlScriptEditorControl)}. Actual type: {docViewObj.GetType().Name}");
            throw new Exception($"Current active frame content is not {nameof(SqlScriptEditorControl)}");
        }
        return editorControl;
    }

    /// <summary>
    /// Получаем вкладку, на которой спозиционирован юзер
    /// </summary>
    private IVsWindowFrame GetOnScreenWindowFrame()
    {
        try
        {
            logger.Debug("Getting active window frame...");

            ThreadHelper.ThrowIfNotOnUIThread();

            var shell = ServiceCache.VsUIShell;
            if (shell == null)
            {
                logger.Error("VS UIShell service not available");
                throw new Exception("VS UIShell service not available");
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

                frame.IsOnScreen(out var onScreenFrame);
                if (onScreenFrame == 1)
                {
                    logger.Info($"Active window frame found (checked {frameCount} frames)");
                    return frame;
                }
            }

            logger.Error($"Active window frame not found (checked {frameCount} frames)");
            throw new Exception("Active window frame not found");
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to get active window frame");
            throw new Exception("Failed to get active window frame", ex);
        }
    }

    private object GetGridResultPage()
    {
        try
        {
            logger.Debug("Getting grid result page...");

            var editorControl = GetSqlScriptEditorControl();

            if (editorControl == null)
            {
                logger.Error($"{nameof(editorControl)} is null");
                throw new Exception($"{nameof(editorControl)} is null");
            }

            // Получим сам контролл с вкладками Results, Messages и тд
            var sqlResultsControl = editorControl.GetType()
                .GetField("m_sqlResultsControl", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(editorControl);

            if (sqlResultsControl == null)
            {
                logger.Error("SQL results control not found in editor");
                throw new Exception("SQL results control not found in editor");
            }

            // Получим вкладку с Results
            var gridResultsPage = sqlResultsControl.GetType()
                .GetField("m_gridResultsPage", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(sqlResultsControl);

            logger.Info(gridResultsPage != null
                ? "Grid results page found successfully"
                : "Grid results page not found");

            return gridResultsPage;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to get grid result page");
            throw new Exception("Failed to get grid result page", ex);
        }
    }
}
