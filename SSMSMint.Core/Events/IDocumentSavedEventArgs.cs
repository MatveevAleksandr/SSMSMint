using SSMSMint.Core.Interfaces;

namespace SSMSMint.Core.Events;

public interface IDocumentSavedEventArgs
{
    public ITextDocumentManager TextDocumentManager { get; }
}
