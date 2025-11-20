using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using NLog;
using SSMSMint.Shared;
using SSMSMint.Shared.Services;
using System;
using System.ComponentModel.Design;
using Task = System.Threading.Tasks.Task;

namespace SSMSMint.CommentToggle;

internal class CommentToggleCommand
{
    /// <summary>
    /// Command ID.
    /// </summary>
    public const int CommandId = 0x107;

    /// <summary>
    /// Command menu group (command set GUID).
    /// </summary>
    public static readonly Guid CommandSet = GUIDs.CommandSetGUID;

    /// <summary>
    /// VS Package that provides this command, not null.
    /// </summary>
    private readonly AsyncPackage package;

    private readonly Logger _logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Initializes a new instance of the <see cref="CommentToggleCommand"/> class.
    /// Adds our command handlers for menu (commands must exist in the command table file)
    /// </summary>
    /// <param name="package">Owner package, not null.</param>
    /// <param name="commandService">Command service to add command to, not null.</param>
    private CommentToggleCommand(AsyncPackage package, OleMenuCommandService commandService)
    {
        this.package = package ?? throw new ArgumentNullException(nameof(package));
        commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

        var menuCommandID = new CommandID(CommandSet, CommandId);
        var menuItem = new OleMenuCommand(Execute, menuCommandID);

        commandService.AddCommand(menuItem);
    }

    /// <summary>
    /// Gets the instance of the command.
    /// </summary>
    public static CommentToggleCommand Instance
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
        // Switch to the main thread - the call to AddCommand in CommentToggleCommand's constructor requires
        // the UI thread.
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

        OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
        Instance = new CommentToggleCommand(package, commandService);
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
            var dte = (DTE2)await package.GetServiceAsync(typeof(DTE)) ?? throw new Exception("DTE core not found");
            var textDocument = (TextDocument)dte.ActiveDocument.Object("TextDocument");
            var selection = textDocument.Selection;

            if (selection == null)
                return;

            var lineStart = selection.TopPoint.Line;
            var colStart = selection.TopPoint.LineCharOffset;
            var lineEnd = selection.BottomPoint.Line;
            var colEnd = selection.BottomPoint.LineCharOffset;

            var commented = CommentService.IsTextCommented(textDocument, lineStart, colStart, lineEnd, colEnd, out var _);

            if (commented)
            {
                CommentService.UncommentText(textDocument, lineStart, colStart, lineEnd, colEnd);
            }
            else
            {
                if (selection.IsEmpty)
                {
                    CommentService.CommentText(textDocument, lineStart, colStart, lineEnd, colEnd, CommentService.CommentType.LineComment);
                }
                else
                {
                    CommentService.CommentText(textDocument, lineStart, colStart, lineEnd, colEnd, CommentService.CommentType.SurroundedComment);
                    var endPoint = selection.BottomPoint.CreateEditPoint(); // selection moved
                    selection.MoveTo(lineStart, colStart);
                    selection.MoveToPoint(endPoint, true);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex);

            VsShellUtilities.ShowMessageBox(
                package,
                ex.Message,
                "Error",
                OLEMSGICON.OLEMSGICON_CRITICAL,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }
    }
}
