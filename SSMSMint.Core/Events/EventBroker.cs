using SSMSMint.Core.Interfaces;
using System;

namespace SSMSMint.Core.Events;

/// <summary>
/// Брокер, который реагирует на события посылаемые из студии
/// </summary>
public class EventBroker
{
    public event EventHandler<IWindowCreatedEventArgs> WindowCreated;
    public event EventHandler<IDocumentSavedEventArgs> DocumentSaved;
    public event EventHandler<IEditorTextChangedEventArgs> EditorTextChanged;

    public void RaiseWindowCreated(IWindowCreatedEventArgs winArgs)
    {
        WindowCreated?.Invoke(this, winArgs);
    }

    public void RaiseDocumentSaved(IDocumentSavedEventArgs docArgs)
    {
        DocumentSaved?.Invoke(this, docArgs);
    }

    public void RaiseEditorTextChanged(IEditorTextChangedEventArgs edArgs)
    {
        EditorTextChanged?.Invoke(this, edArgs);
    }
}
