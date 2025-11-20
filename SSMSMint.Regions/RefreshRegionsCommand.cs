using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using NLog;
using SSMSMint.Shared;
using SSMSMint.Shared.Settings;
using System;
using System.ComponentModel.Design;
using Task = System.Threading.Tasks.Task;

namespace SSMSMint.Regions;

internal sealed class RefreshRegionsCommand
{
    /// <summary>
    /// Command ID.
    /// </summary>
    public const int CommandId = 0x103;

    /// <summary>
    /// Command menu group (command set GUID).
    /// </summary>
    public static readonly Guid CommandSet = GUIDs.CommandSetGUID;

    /// <summary>
    /// VS Package that provides this command, not null.
    /// </summary>
    private readonly AsyncPackage package;

    /// <summary>
    /// Initializes a new instance of the <see cref="RefreshRegionsCommand"/> class.
    /// Adds our command handlers for menu (commands must exist in the command table file)
    /// </summary>
    /// <param name="package">Owner package, not null.</param>
    /// <param name="commandService">Command service to add command to, not null.</param>
    private RefreshRegionsCommand(AsyncPackage package, OleMenuCommandService commandService)
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
            ((OleMenuCommand)sender).Enabled = settings?.RegionsEnabled ?? false;
        }
        catch (Exception ex)
        {
            LogManager.GetCurrentClassLogger().Error(ex);
        }
    }

    /// <summary>
    /// Gets the instance of the command.
    /// </summary>
    public static RefreshRegionsCommand Instance
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
        // Switch to the main thread - the call to AddCommand in RefreshRegionsCommand's constructor requires
        // the UI thread.
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

        OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
        Instance = new RefreshRegionsCommand(package, commandService);
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
            var textDocument = (TextDocument)dte.ActiveDocument.Object("TextDocument");
            textDocument.CreateCustomRegions(settings);
        }
        catch (Exception ex)
        {
            LogManager.GetCurrentClassLogger().Error(ex);

            VsShellUtilities.ShowMessageBox(
                package,
                ex.Message,
                "Refreshing regions error",
                OLEMSGICON.OLEMSGICON_CRITICAL,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }
    }
}

