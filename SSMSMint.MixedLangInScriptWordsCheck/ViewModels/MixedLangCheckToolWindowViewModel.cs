using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using NLog;
using SSMSMint.MixedLangInScriptWordsCheck.Models;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Task = System.Threading.Tasks.Task;

namespace SSMSMint.MixedLangInScriptWordsCheck.ViewModels;

internal class MixedLangCheckToolWindowViewModel : INotifyPropertyChanged
{
    private IEnumerable<MixedLangWord> _mixedLangWords;
    public IEnumerable<MixedLangWord> MixedLangWords
    {
        get
        {
            return _mixedLangWords;
        }
        set
        {
            _mixedLangWords = value;
            OnPropertyChanged(nameof(MixedLangWords));
            OnPropertyChanged(nameof(MixedLangWordsCount));
        }
    }
    private AsyncPackage _package;
    public int MixedLangWordsCount => MixedLangWords.Count();

    public event PropertyChangedEventHandler PropertyChanged;

    public void InitParams(IEnumerable<MixedLangWord> mixedLangWords, AsyncPackage package)
    {
        MixedLangWords = mixedLangWords;
        _package = package;
    }

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public async Task MixedLangWordItemSelectionChanged(MixedLangWord word)
    {
        var dte = (DTE2)await _package.GetServiceAsync(typeof(DTE));
        var activeDoc = dte?.ActiveDocument;
        if (activeDoc == null)
        {
            LogManager.GetCurrentClassLogger().Error("Try to locate mixed lang word failed. dte or active document not found.");
            return;
        }

        var selection = (TextSelection)activeDoc.Selection;
        var line = word.LineIndex + 1;
        var column = word.ColumnIndex + 1;

        selection.MoveToLineAndOffset(line, column, false);
        selection.MoveToLineAndOffset(line, column + word.Word.Length, true);
        dte.ActiveDocument.Activate();
    }
}
