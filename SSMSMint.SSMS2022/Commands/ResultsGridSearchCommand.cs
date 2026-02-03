using SSMSMint.Core;
using SSMSMint.Core.Interfaces;
using SSMSMint.Core.UI.Models;
using SSMSMint.SSMS2022.ToolWindows;
using Microsoft.VisualStudio.Shell;
using NLog;
using System;
using System.ComponentModel.Design;
using System.Threading.Tasks;

namespace SSMSMint.SSMS2022.Commands;

public sealed class ResultsGridSearchCommand
{
    /// <summary>
    /// Command ID.
    /// </summary>
    public const int CommandId = 0x100;

    /// <summary>
    /// Command menu group (command set GUID).
    /// </summary>
    public static readonly Guid CommandSet = Consts.CommandSetGUID;

    private readonly Logger logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// VS Package that provides this command, not null.
    /// </summary>
    private readonly AsyncPackage package;
    private readonly IUINotificationManager uiNotificationManager;
    private readonly IWorkspaceManager wManager;
    private readonly IResultsGridSearchFeature feature;
    private readonly ISettingsManager settingsManager;
    private readonly string themeUriStr;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResultsGridSearchCommand"/> class.
    /// Adds our command handlers for menu (commands must exist in the command table file)
    /// </summary>
    /// <param name="package">Owner package, not null.</param>
    /// <param name="commandService">Command service to add command to, not null.</param>
    private ResultsGridSearchCommand(AsyncPackage package, OleMenuCommandService commandService, IUINotificationManager uiNotificationManager, IWorkspaceManager wManager, IResultsGridSearchFeature feature, ISettingsManager settingsManager, string themeUriStr)
    {
        this.package = package ?? throw new ArgumentNullException(nameof(package));
        this.uiNotificationManager = uiNotificationManager ?? throw new ArgumentNullException(nameof(uiNotificationManager));
        this.feature = feature ?? throw new ArgumentNullException(nameof(feature));
        this.wManager = wManager ?? throw new ArgumentNullException(nameof(wManager));
        this.settingsManager = settingsManager ?? throw new ArgumentNullException(nameof(settingsManager));
        this.themeUriStr = themeUriStr ?? throw new ArgumentNullException(nameof(themeUriStr));
        commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

        var menuCommandID = new CommandID(CommandSet, CommandId);
        var menuItem = new OleMenuCommand(Execute, menuCommandID);

        menuItem.BeforeQueryStatus += MenuItem_BeforeQueryStatus;
        commandService.AddCommand(menuItem);

        logger.Info($"{nameof(ResultsGridSearchCommand)} Initialized");
    }

    private void MenuItem_BeforeQueryStatus(object sender, EventArgs e)
    {
        try
        {
            var settings = settingsManager.GetSettings() ?? throw new Exception("Settings not found");
            ((OleMenuCommand)sender).Enabled = settings?.ResultsGridSearchEnabled ?? false;
        }
        catch (Exception ex)
        {
            LogManager.GetCurrentClassLogger().Error(ex);
        }
    }

    /// <summary>
    /// Gets the instance of the command.
    /// </summary>
    public static ResultsGridSearchCommand Instance
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
    public static async Task InitializeAsync(AsyncPackage package, IUINotificationManager uiNotificationManager, IWorkspaceManager wManager, IResultsGridSearchFeature feature, ISettingsManager settingsManager, string themeUriStr)
    {
        // Switch to the main thread - the call to AddCommand in ResultsGridSearchToolWindowCommand's constructor requires
        // the UI thread.
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

        OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
        Instance = new ResultsGridSearchCommand(package, commandService, uiNotificationManager, wManager, feature, settingsManager, themeUriStr);
    }

    /// <summary>
    /// Shows the tool window when the menu item is clicked.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event args.</param>
    private async void Execute(object sender, EventArgs e)
    {
        try
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var par = new ResultsGridsSearchToolWindowParams(feature, uiNotificationManager, themeUriStr);
            await wManager.ShowToolWindowAsync<ResultsGridSearchToolWindow>(par);
        }
        catch (Exception ex)
        {
            logger.Error(ex);
            uiNotificationManager.ShowError("Results grid search error", ex.Message);
        }
    }
}
