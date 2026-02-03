using SSMSMint.Core.Helpers;
using SSMSMint.Core.Interfaces;
using NLog;
using System.Linq;
using System.Threading.Tasks;

namespace SSMSMint.Features;

public class LocateInObjectExplorerFeature(
    IWorkspaceManager workspaceManager,
    IUINotificationManager uINotificationManager,
    IObjectExplorerManager oeManager
    )
{
    private readonly Logger logger = LogManager.GetCurrentClassLogger();

    public async Task Process(ITextDocumentManager tdManager)
    {
        var editorConnection = workspaceManager.GetActiveEditorConnection();
        var scriptText = await tdManager.GetFullTextAsync();
        var position = await tdManager.GetCaretPositionAsync();
        var sqlObj = ScriptDomSqlAnalyzerHelper.GetSqlObjectAtPosition(scriptText, position, editorConnection.ServerName, editorConnection.DatabaseName, out var parseErrors);

        if (parseErrors.Count > 0)
        {
            logger.Warn("Errors while parsing SQL script:\n" +
                    string.Join("\n", parseErrors.Select(e => $"[{e.Line},{e.Column}]: {e.Message}")));
        }

        if (sqlObj == null)
        {
            uINotificationManager.ShowWarning("Locating in object explorer", "SQL Object to locate is not defined");
            return;
        }

        oeManager.ConnectToServer(editorConnection);

        if (!await oeManager.TryFindObjNodeAsync(sqlObj))
        {
            uINotificationManager.ShowWarning("Locating in object explorer", $"SQL object '{sqlObj.ObjName}' not found");
        }
    }
}

