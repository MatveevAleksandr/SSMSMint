using SSMSMint.Core.Helpers;
using SSMSMint.Core.Interfaces;
using SSMSMint.Core.Models;
using NLog;
using System.Linq;
using System.Threading.Tasks;

namespace SSMSMint.Features;

public class ScriptSqlObjectFeature(
    IWorkspaceManager workspaceManager,
    ISettingsManager settingsManager,
    IUINotificationManager uINotificationManager,
    RegionsFeature regionsFeature
    )
{
    private readonly Logger logger = LogManager.GetCurrentClassLogger();

    public async Task ProcessAsync(ITextDocumentManager tdManager, ISqlScriptProcessorManager spManager)
    {
        var editorConnection = workspaceManager.GetActiveEditorConnection();
        var settings = settingsManager.GetSettings();
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
            uINotificationManager.ShowWarning("Scripting SQL object", "SQL Object is not defined");
            return;
        }

        var script = spManager.GetSqlObjectScript(sqlObj, editorConnection);

        if (string.IsNullOrEmpty(script))
        {
            uINotificationManager.ShowWarning("Scripting SQL object", $"SQL object not found. {sqlObj.ObjName}");
            return;
        }

        var newTdManager = workspaceManager.CreateNewSqlBlankScript();
        var span = new TextSpan(new TextPoint(1, 1), new TextPoint(1, 1));
        await newTdManager.ReplaceTextAsync(span, script);
        await regionsFeature.CreateCustomRegionsAsync(newTdManager);
        await newTdManager.SetSelectionAsync(span);
    }
}
