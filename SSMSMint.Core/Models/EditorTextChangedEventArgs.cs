using SSMSMint.Core.Interfaces;

namespace SSMSMint.Core.Models;

public class EditorTextChangedEventArgs(ITextDocumentManager textDocumentManager, ITextMarkingManager textMarkingManager) : IEditorTextChangedEventArgs
{
    public ITextDocumentManager TextDocumentManager => textDocumentManager;

    public ITextMarkingManager TextMarkingManager => textMarkingManager;
}
