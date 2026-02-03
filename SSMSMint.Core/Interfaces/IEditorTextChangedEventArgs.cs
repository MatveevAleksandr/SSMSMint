namespace SSMSMint.Core.Interfaces;

public interface IEditorTextChangedEventArgs
{
    public ITextDocumentManager TextDocumentManager { get; }
    public ITextMarkingManager TextMarkingManager { get; }
}
