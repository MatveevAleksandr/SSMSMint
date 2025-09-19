using Microsoft.VisualStudio;
using Microsoft.VisualStudio.TextManager.Interop;

namespace SSMSMint.TextMarker;

internal sealed class TextMarkerClient(string tooltip) : IVsTextMarkerClient
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