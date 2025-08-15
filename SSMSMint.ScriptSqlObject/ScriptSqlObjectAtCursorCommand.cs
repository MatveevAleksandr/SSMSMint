using EnvDTE;
using EnvDTE80;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SqlServer.Management.UI.VSIntegration;
using Microsoft.SqlServer.Management.UI.VSIntegration.Editors;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using NLog;
using SSMSMint.Regions;
using SSMSMint.Shared.Extentions;
using SSMSMint.Shared.Services;
using SSMSMint.Shared.Settings;
using SSMSMint.Shared.SqlObjAtPosition;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using Task = System.Threading.Tasks.Task;

namespace SSMSMint.ScriptSqlObject
{
    internal sealed class ScriptSqlObjectAtCursorCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x102;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("E9307D44-1C11-44C0-937E-A66F19EA3B26");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly string _commandTitle = "Scripting SQL object";

        /// <summary>
        /// Initializes a new instance of the <see cref="LocateInObjectExplorerCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private ScriptSqlObjectAtCursorCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new OleMenuCommand(Execute, menuCommandID);

            menuItem.BeforeQueryStatus += MenuItem_BeforeQueryStatus;
            commandService.AddCommand(menuItem);
        }

        private void MenuItem_BeforeQueryStatus(object sender, EventArgs e)
        {
            try
            {
                var settings = (SSMSMintSettings)package.GetDialogPage(typeof(SSMSMintSettings)) ?? throw new Exception("Settings not found");
                ((OleMenuCommand)sender).Enabled = settings?.ScriptSqlObjectEnabled ?? false;
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static ScriptSqlObjectAtCursorCommand Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        //private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider
        //{
        //    get
        //    {
        //        return this.package;
        //    }
        //}

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread - the call to AddCommand in LocateInObjectExplorerCommand's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new ScriptSqlObjectAtCursorCommand(package, commandService);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private async void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            try
            {
                var settings = (SSMSMintSettings)package.GetDialogPage(typeof(SSMSMintSettings)) ?? throw new Exception("Settings not found");
                var dte = (DTE2)await package.GetServiceAsync(typeof(DTE)) ?? throw new Exception("DTE core not found");
                var frameService = ServicesLocator.ServiceProvider.GetRequiredService<FrameService>();
                var editor = frameService.GetSqlScriptEditorControl() ?? throw new Exception("SqlScriptEditorControl not found");
                var editorConnection = editor.GetSqlConnection() ?? throw new Exception("Editor SQLConnection not found");
                var ts = (TextSelection)dte.ActiveDocument.Selection;

                editor.GetSqlObjectAtPosition(ts.CurrentLine, ts.CurrentColumn, out IList<ParseError> parseErrors, out SqlObject sqlObj);

                if (parseErrors.Count > 0)
                {
                    _logger.Warn("Errors while parsing SQL script:\n" +
                            string.Join("\n", parseErrors.Select(e => $"[{e.Line},{e.Column}]: {e.Message}")));
                }

                if (sqlObj == null)
                {
                    ShowWarning("SQL Object is not defined");
                    return;
                }

                _logger.Info($"SQL obj params: Type - {sqlObj.GetType()}; {sqlObj.GetParamsString()}");

                if (!ScriptProcessor.TryProcess(sqlObj, editorConnection.ConnectionString, out var sqlScript))
                {
                    ShowWarning($"SQL object not found. {sqlObj.GetParamsString()}");
                    return;
                }

                var connInfo = ServiceCache.ScriptFactory.CurrentlyActiveWndConnectionInfo.UIConnectionInfo;
                connInfo.ServerName = sqlObj.ContextServerName;

                ServiceCache.ScriptFactory.CreateNewBlankScript(ScriptType.Sql, connInfo, null);
                var newDoc = (TextDocument)dte.ActiveDocument.Object("TextDocument");
                newDoc.Selection?.Insert(sqlScript);
                newDoc.CreateCustomRegions(settings);
                newDoc.Selection?.StartOfDocument();
            }
            catch (Exception ex)
            {
                _logger.Error(ex);

                VsShellUtilities.ShowMessageBox(
                    package,
                    ex.Message,
                    "Error scripting sql object",
                    OLEMSGICON.OLEMSGICON_CRITICAL,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
        }

        private void ShowWarning(string body)
        {
            VsShellUtilities.ShowMessageBox(
                            this.package,
                            body,
                            _commandTitle,
                            OLEMSGICON.OLEMSGICON_WARNING,
                            OLEMSGBUTTON.OLEMSGBUTTON_OK,
                            OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }
    }
}

