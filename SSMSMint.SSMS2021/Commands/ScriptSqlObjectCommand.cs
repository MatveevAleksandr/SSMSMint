using EnvDTE;
using EnvDTE80;
using SSMSMint.Core;
using SSMSMint.Core.Interfaces;
using SSMSMint.Features;
using SSMSMint.SSMS2021.Implementations;
using Microsoft.VisualStudio.Shell;
using NLog;
using System;
using System.ComponentModel.Design;
using System.Threading.Tasks;

namespace SSMSMint.SSMS2021.Commands;

internal sealed class ScriptSqlObjectCommand
{
    /// <summary>
    /// Command ID.
    /// </summary>
    public const int CommandId = 0x102;

    /// <summary>
    /// Command menu group (command set GUID).
    /// </summary>
    public static readonly Guid CommandSet = Consts.CommandSetGUID;

    /// <summary>
    /// VS Package that provides this command, not null.
    /// </summary>
    private readonly AsyncPackage package;
    private readonly IUINotificationManager uiNotificationManager;
    private readonly ISqlScriptProcessorManager sqlScriptProcessorManager;
    private readonly ISettingsManager settingsManager;
    private readonly ScriptSqlObjectFeature feature;

    private readonly Logger logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Initializes a new instance of the <see cref="LocateInObjectExplorerCommand"/> class.
    /// Adds our command handlers for menu (commands must exist in the command table file)
    /// </summary>
    /// <param name="package">Owner package, not null.</param>
    /// <param name="commandService">Command service to add command to, not null.</param>
    private ScriptSqlObjectCommand(
        AsyncPackage package,
        OleMenuCommandService commandService,
        IUINotificationManager uiNotificationManager,
        ISqlScriptProcessorManager sqlScriptProcessorManager,
        ISettingsManager settingsManager,
        ScriptSqlObjectFeature feature)
    {
        this.package = package ?? throw new ArgumentNullException(nameof(package));
        this.uiNotificationManager = uiNotificationManager ?? throw new ArgumentNullException(nameof(uiNotificationManager));
        this.sqlScriptProcessorManager = sqlScriptProcessorManager ?? throw new ArgumentNullException(nameof(sqlScriptProcessorManager));
        this.settingsManager = settingsManager ?? throw new ArgumentNullException(nameof(settingsManager));
        this.feature = feature ?? throw new ArgumentNullException(nameof(feature));
        commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

        var menuCommandID = new CommandID(CommandSet, CommandId);
        var menuItem = new OleMenuCommand(Execute, menuCommandID);

        menuItem.BeforeQueryStatus += MenuItem_BeforeQueryStatus;
        commandService.AddCommand(menuItem);

        logger.Info($"{nameof(ScriptSqlObjectCommand)} Initialized");
    }

    private void MenuItem_BeforeQueryStatus(object sender, EventArgs e)
    {
        try
        {
            var settings = settingsManager.GetSettings() ?? throw new Exception("Settings not found");
            ((OleMenuCommand)sender).Enabled = settings?.ScriptSqlObjectEnabled ?? false;
        }
        catch (Exception ex)
        {
            logger.Error(ex);
        }
    }

    /// <summary>
    /// Gets the instance of the command.
    /// </summary>
    public static ScriptSqlObjectCommand Instance
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
    public static async Task InitializeAsync(AsyncPackage package, IUINotificationManager uiNotificationManager, ISqlScriptProcessorManager sqlScriptProcessorManager, ISettingsManager settingsManager, ScriptSqlObjectFeature feature)
    {
        // Switch to the main thread - the call to AddCommand in LocateInObjectExplorerCommand's constructor requires
        // the UI thread.
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

        OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
        Instance = new ScriptSqlObjectCommand(package, commandService, uiNotificationManager, sqlScriptProcessorManager, settingsManager, feature);
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
        try
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var dte = (DTE2)await package.GetServiceAsync(typeof(DTE)) ?? throw new Exception("DTE core not found");
            var doc = (TextDocument)dte.ActiveDocument.Object("TextDocument") ?? throw new Exception("ActiveDocument not found");
            var tdManager = new TextDocumentManagerImpl(doc);
            await feature.ProcessAsync(tdManager, sqlScriptProcessorManager);
        }
        catch (Exception ex)
        {
            logger.Error(ex);
            uiNotificationManager.ShowError("Scripting SQL object", ex.Message);
        }
    }
}
