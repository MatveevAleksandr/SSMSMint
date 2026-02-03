using SSMSMint.Core.Events;
using SSMSMint.Core.Interfaces;

namespace SSMSMint.Core.Models;

public class DocumentSavedEventArgs(ITextDocumentManager textDocumentManager) : IDocumentSavedEventArgs
{
    public ITextDocumentManager TextDocumentManager => textDocumentManager;
}
