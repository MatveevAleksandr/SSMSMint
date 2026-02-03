using SSMSMint.Core;
using SSMSMint.Core.Interfaces;
using SSMSMint.Features;
using Microsoft.VisualStudio.Shell;
using NLog;
using System;
using System.ComponentModel.Design;
using Task = System.Threading.Tasks.Task;

namespace SSMSMint.SSMS2019.Commands;

public sealed class CopySelectedHeadersCommand
{
    /// <summary>
    /// Command ID.
    /// </summary>
    public const int CommandId = 0x106;

    /// <summary>
    /// Command menu group (command set GUID).
    /// </summary>
    public static readonly Guid CommandSet = Consts.CommandSetGUID;

    /// <summary>
    /// VS Package that provides this command, not null.
    /// </summary>
    private readonly AsyncPackage package;
    private readonly IUINotificationManager uiNotificationManager;
    private readonly CopyHeadersFeature feature;
    private readonly IWorkspaceManager workspaceManager;

    private readonly Logger logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Initializes a new instance of the <see cref="CopySelectedHeadersCommand"/> class.
    /// Adds our command handlers for menu (commands must exist in the command table file)
    /// </summary>
    /// <param name="package">Owner package, not null.</param>
    /// <param name="commandService">Command service to add command to, not null.</param>
    private CopySelectedHeadersCommand(AsyncPackage package, OleMenuCommandService commandService, CopyHeadersFeature feature, IWorkspaceManager workspaceManager, IUINotificationManager uiNotificationManager)
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
    public static CopySelectedHeadersCommand Instance
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
    public static async Task InitializeAsync(AsyncPackage package, CopyHeadersFeature feature, IWorkspaceManager workspaceManager, IUINotificationManager uiNotificationManager)
    {
        // Switch to the main thread - the call to AddCommand in CopySelectedHeadersCommand's constructor requires
        // the UI thread.
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

        OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
        Instance = new CopySelectedHeadersCommand(package, commandService, feature, workspaceManager, uiNotificationManager);
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
            feature.CopyHeadersToClipboard(grManager, true);
        }
        catch (Exception ex)
        {
            logger.Error(ex);
            uiNotificationManager.ShowError("Ошибка копирования", ex.Message);
        }
    }
}
