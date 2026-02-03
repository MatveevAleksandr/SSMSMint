using SSMSMint.Core.Events;
using SSMSMint.Core.Interfaces;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using System;

namespace SSMSMint.SSMS2020.Infrastructure;

public class VsTextLinesEventsListener : IVsTextLinesEvents, IDisposable
{
    private readonly IConnectionPoint connectionPoint;
    private readonly uint cookie;
    private bool disposed;
    private readonly EventBroker eventBroker;
    private readonly IEditorTextChangedEventArgs tArgs;

    public VsTextLinesEventsListener(IEditorTextChangedEventArgs tArgs, IVsTextLines lines, EventBroker eventBroker)
    {
        Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

        if (lines is not IConnectionPointContainer container)
            throw new InvalidCastException("IVsTextLines не поддерживает IConnectionPointContainer");

        Guid guid = typeof(IVsTextLinesEvents).GUID;
        container.FindConnectionPoint(ref guid, out connectionPoint);
        connectionPoint.Advise(this, out cookie);
        this.eventBroker = eventBroker;
        this.tArgs = tArgs;
    }

    public void OnChangeLineText(TextLineChange[] pTextLineChange, int fLast)
    {
        eventBroker.RaiseEditorTextChanged(tArgs);
    }

    public void Dispose()
    {
        if (!disposed)
        {
            connectionPoint?.Unadvise(cookie);
            disposed = true;
        }
        GC.SuppressFinalize(this);
    }

    public void OnChangeLineAttributes(int iFirstLine, int iLastLine) { }
}
