using Microsoft.SqlServer.TransactSql.ScriptDom;
using Microsoft.VisualStudio.TextManager.Interop;
using System.Collections.Generic;
using System.IO;

namespace SSMSMint.TextMarker;

public class TextMarkerTagger
{
    private readonly List<IVsTextLineMarker> _activeLineMarkers = new();
    private readonly TSql150Parser _parser = new(true);

    private readonly TextMarkerClient _notUsedVarsMarkerClient = new("The variable is declared, but not used");
    private readonly TextMarkerClient _notDeclaredVarsMarkerClient = new("The variable is not declared");

    public void RefreshTextMarkers(IVsTextLines lines)
    {
        if (lines is null)
            return;

        lines.GetLastLineIndex(out int lastLine, out int lastIndex);
        lines.GetLineText(0, 0, lastLine, lastIndex, out string scriptText);

        var parsedSqlScript = (TSqlScript)_parser.Parse(new StringReader(scriptText), out var _);

        if (parsedSqlScript is null)
            return;

        ClearCreatedMarkers();

        foreach (var batch in parsedSqlScript.Batches)
        {
            var visitor = new TextVisitorByBatch();
            batch.Accept(visitor);

            foreach (var markerObject in visitor.NotUsedVars)
            {
                CreateLineMarker(lines, _notUsedVarsMarkerClient, markerObject);
            }

            foreach (var markerObject in visitor.NotDeclaredVars)
            {
                CreateLineMarker(lines, _notDeclaredVarsMarkerClient, markerObject);
            }
        }
    }

    private void CreateLineMarker(IVsTextLines lines, IVsTextMarkerClient client, TextMarkerObject markerObject)
    {
        IVsTextLineMarker[] markers = new IVsTextLineMarker[1];
        lines.CreateLineMarker((int)MARKERTYPE.MARKER_OTHER_ERROR,
            markerObject.StartLine - 1,
            markerObject.StartIndex - 1,
            markerObject.EndLine - 1,
            markerObject.EndIndex - 1, client, markers);
        _activeLineMarkers.Add(markers[0]);
    }

    private void ClearCreatedMarkers()
    {
        foreach (var marker in _activeLineMarkers)
        {
            marker.Invalidate();
        }
        _activeLineMarkers.Clear();
    }
}