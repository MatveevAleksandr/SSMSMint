using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using SSMSMint.Shared.Extentions;
using SSMSMint.Shared.Services;
using System;
using System.ComponentModel.Design;
using Task = System.Threading.Tasks.Task;

namespace SSMSMint.ViewGridCellAsJson;

/// <summary>
/// Command handler
/// </summary>
public sealed class ViewGridCellAsJsonCommand
{
    /// <summary>
    /// Command ID.
    /// </summary>
    public const int CommandId = 0x104;

    /// <summary>
    /// Command menu group (command set GUID).
    /// </summary>
    public static readonly Guid CommandSet = new Guid("E9307D44-1C11-44C0-937E-A66F19EA3B26");

    /// <summary>
    /// VS Package that provides this command, not null.
    /// </summary>
    private readonly AsyncPackage package;

    private readonly Logger _logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Initializes a new instance of the <see cref="ViewGridCellAsJsonCommand"/> class.
    /// Adds our command handlers for menu (commands must exist in the command table file)
    /// </summary>
    /// <param name="package">Owner package, not null.</param>
    /// <param name="commandService">Command service to add command to, not null.</param>
    private ViewGridCellAsJsonCommand(AsyncPackage package, OleMenuCommandService commandService)
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
    public static async Task InitializeAsync(AsyncPackage package)
    {
        // Switch to the main thread - the call to AddCommand in ViewGridCellAsJsonCommand's constructor requires
        // the UI thread.
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

        OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
        Instance = new ViewGridCellAsJsonCommand(package, commandService);
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
            ThreadHelper.ThrowIfNotOnUIThread();
            var dte = (DTE2)await package.GetServiceAsync(typeof(DTE)) ?? throw new Exception("DTE core not found");
            var gridControl = FrameService.GetLastFocusedOrFirstGridControl() ?? throw new Exception("Grid control is not found");

            // Получим ячейку и ее содержимое
            gridControl.GetCurrentCell(out var rowIndex, out var columnIndex);
            var cellData = gridControl.GridStorage.GetCellDataAsString(rowIndex, columnIndex);
            var colHeader = gridControl.GridStorage.GetColumnHeader(columnIndex);
            if (string.IsNullOrWhiteSpace(colHeader))
            {
                colHeader = "Undefined";
            }

            // Тут проверим на JSON ли. Если нет, то выбросит JsonReaderException
            var parsedJson = JToken.Parse(cellData);
            var formattedData = parsedJson.ToString(Formatting.Indented);

            // Отобразим отформатированный JSON
            dte.ItemOperations.NewFile("General\\Text File", $"{colHeader}_JsonView");
            var newDoc = (TextDocument)dte.ActiveDocument.Object("TextDocument");
            newDoc.Selection?.Insert(formattedData);
            newDoc.Selection?.StartOfDocument();
        }
        catch (JsonReaderException)
        {
            VsShellUtilities.ShowMessageBox(
                package,
                "The contents of the cell are not correct JSON",
                "Warning",
                OLEMSGICON.OLEMSGICON_WARNING,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
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
