using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.Threading;

namespace SSMSMint.TextMarker;

public class VsTextLinesEventsListener : IVsTextLinesEvents, IDisposable
{
    private readonly int _timerDelay_ms = 1000;
    private Timer _textChangingTimer;
    private readonly object _timerLock = new();

    private readonly IConnectionPoint _connectionPoint;
    private readonly uint _cookie;
    private bool _disposed;

    private readonly TextMarkerTagger _tagger;
    private readonly IVsTextLines _lines;

    public VsTextLinesEventsListener(IVsTextLines lines, TextMarkerTagger tagger)
    {
        if (lines is not IConnectionPointContainer container)
            throw new InvalidCastException("IVsTextLines is not IConnectionPointContainer");

        Guid guid = typeof(IVsTextLinesEvents).GUID;
        container.FindConnectionPoint(ref guid, out _connectionPoint);

        _connectionPoint.Advise(this, out _cookie);
        _tagger = tagger;
        _lines = lines;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _connectionPoint?.Unadvise(_cookie);
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }

    public void OnChangeLineText(TextLineChange[] pTextLineChange, int fLast)
    {
        lock (_timerLock)
        {
            _textChangingTimer?.Dispose();

            _textChangingTimer = new(_ =>
            {
                _tagger.RefreshTextMarkers(_lines);
            }, null, _timerDelay_ms, Timeout.Infinite);
        }
    }

    public void OnChangeLineAttributes(int iFirstLine, int iLastLine) { }
}
