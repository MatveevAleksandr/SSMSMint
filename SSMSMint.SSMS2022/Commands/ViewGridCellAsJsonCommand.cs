using SSMSMint.Core;
using SSMSMint.Core.Interfaces;
using SSMSMint.Features;
using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json;
using NLog;
using System;
using System.ComponentModel.Design;
using System.Threading.Tasks;

namespace SSMSMint.SSMS2022.Commands;

internal class ViewGridCellAsJsonCommand
{
    /// <summary>
    /// Command ID.
    /// </summary>
    public const int CommandId = 0x104;

    /// <summary>
    /// Command menu group (command set GUID).
    /// </summary>
    public static readonly Guid CommandSet = Consts.CommandSetGUID;

    /// <summary>
    /// VS Package that provides this command, not null.
    /// </summary>
    private readonly AsyncPackage package;
    private readonly IUINotificationManager uiNotificationManager;
    private readonly IWorkspaceManager workspaceManager;
    private readonly ViewGridCellAsJsonFeature feature;

    private readonly Logger logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Initializes a new instance of the <see cref="ViewGridCellAsJsonCommand"/> class.
    /// Adds our command handlers for menu (commands must exist in the command table file)
    /// </summary>
    /// <param name="package">Owner package, not null.</param>
    /// <param name="commandService">Command service to add command to, not null.</param>
    private ViewGridCellAsJsonCommand(AsyncPackage package, OleMenuCommandService commandService, ViewGridCellAsJsonFeature feature, IWorkspaceManager workspaceManager, IUINotificationManager uiNotificationManager)
    {
        this.package = package ?? throw new ArgumentNullException(nameof(package));
        this.uiNotificationManager = uiNotificationManager ?? throw new ArgumentNullException(nameof(uiNotificationManager));
        this.feature = feature ?? throw new ArgumentNullException(nameof(feature));
        this.workspaceManager = workspaceManager ?? throw new ArgumentNullException(nameof(workspaceManager));
        commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

        var menuCommandID = new CommandID(CommandSet, CommandId);
        var menuItem = new OleMenuCommand(Execute, menuCommandID);

        commandService.AddCommand(menuItem);

        logger.Info($"{nameof(CopySelectedHeadersCommand)} Initialized");
    }

    /// <summary>
    /// Gets the instance of the command.
    /// </summary>
    public static ViewGridCellAsJsonCommand Instance
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
    public static async Task InitializeAsync(AsyncPackage package, ViewGridCellAsJsonFeature feature, IWorkspaceManager workspaceManager, IUINotificationManager uiNotificationManager)
    {
        // Switch to the main thread - the call to AddCommand in ViewGridCellAsJsonCommand's constructor requires
        // the UI thread.
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

        OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
        Instance = new ViewGridCellAsJsonCommand(package, commandService, feature, workspaceManager, uiNotificationManager);
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
            var grManager = workspaceManager.GetLastActiveGridControl();
            await feature.ProcessAsync(grManager);
        }
        catch (JsonReaderException)
        {
            uiNotificationManager.ShowWarning("Warning", "Содержимое ячейки не является корректным JSON");
        }
        catch (Exception ex)
        {
            logger.Error(ex);
            uiNotificationManager.ShowError("Ошибка", ex.Message);
        }
    }
}
