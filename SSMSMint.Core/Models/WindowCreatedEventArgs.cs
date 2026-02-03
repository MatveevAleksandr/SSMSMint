using SSMSMint.Core.Events;
using SSMSMint.Core.Interfaces;

namespace SSMSMint.Core.Models;

public class WindowCreatedEventArgs(ITextDocumentManager textDocumentManager, ITextMarkingManager textMarkingManager) : IWindowCreatedEventArgs
{
    public ITextDocumentManager TextDocumentManager => textDocumentManager;

    public ITextMarkingManager TextMarkingManager => textMarkingManager;
}
