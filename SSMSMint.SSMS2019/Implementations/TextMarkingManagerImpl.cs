using SSMSMint.Core.Interfaces;
using SSMSMint.Core.Models;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.TextManager.Interop;
using System.Collections.Generic;
using System.Linq;

namespace SSMSMint.SSMS2019.Implementations;

internal class TextMarkingManagerImpl(IVsTextLines lines) : ITextMarkingManager
{
    private readonly Dictionary<MarkerKind, List<IVsTextLineMarker>> activeMarkerGroups = new();

    public void ApplyMarkers(MarkersGroupDefinition markersGroup)
    {
        var markerKind = markersGroup.Kind;

        // гарантируем, что не останется "фантомных" маркеров
        ClearMarkers(markerKind);

        if (!markersGroup.Spans.Any())
            return;

        var markerType = (int)MapMarkerKindToVsMarkerType(markerKind);
        var client = new TextMarkerClient(markersGroup.ToolTip);
        var newVsMarkers = new List<IVsTextLineMarker>();

        foreach (var span in markersGroup.Spans)
        {
            var sp = span.Start;
            var ep = span.End;

            IVsTextLineMarker[] m = new IVsTextLineMarker[1];
            lines.CreateLineMarker(markerType,
                sp.Line,
                sp.Column,
                ep.Line,
                ep.Column, client, m);

            if (m[0] != null)
                newVsMarkers.Add(m[0]);
        }

        if (newVsMarkers.Any())
            activeMarkerGroups[markerKind] = newVsMarkers;
    }

    private void ClearMarkers(MarkerKind kind)
    {
        if (activeMarkerGroups.TryGetValue(kind, out var markers))
        {
            foreach (var marker in markers)
            {
                marker.Invalidate();
            }
            activeMarkerGroups.Remove(kind);
        }
    }

    public void ClearAllMarkers()
    {
        var keys = activeMarkerGroups.Keys.ToList();
        foreach (var key in keys)
        {
            ClearMarkers(key);
        }
    }

    private MARKERTYPE MapMarkerKindToVsMarkerType(MarkerKind markerKind)
    {
        return markerKind switch
        {
            MarkerKind.NotDeclaredVars => MARKERTYPE.MARKER_OTHER_ERROR,
            MarkerKind.NotUsedVars => MARKERTYPE.MARKER_OTHER_ERROR,
            _ => MARKERTYPE.MARKER_INVISIBLE,
        };
    }

    private sealed class TextMarkerClient(string tooltip) : IVsTextMarkerClient
    {
        public int GetTipText(IVsTextMarker pMarker, string[] pbstrText)
        {
            pbstrText[0] = tooltip;
            return VSConstants.S_OK;
        }

        public void MarkerInvalidated() { }

        public void OnBufferSave(string pszFileName) { }

        public void OnBeforeBufferClose() { }

        public int GetMarkerCommandInfo(IVsTextMarker pMarker, int iItem, string[] pbstrText, uint[] pcmdf) => VSConstants.S_OK;

        public int ExecMarkerCommand(IVsTextMarker pMarker, int iItem) => VSConstants.S_OK;

        public void OnAfterSpanReload() { }

        public int OnAfterMarkerChange(IVsTextMarker pMarker) => VSConstants.S_OK;
    }
}
