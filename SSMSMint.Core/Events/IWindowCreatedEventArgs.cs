using SSMSMint.Core.Interfaces;

namespace SSMSMint.Core.Events;

public interface IWindowCreatedEventArgs
{
    public ITextDocumentManager TextDocumentManager { get; }
    public ITextMarkingManager TextMarkingManager { get; }
}
