using SSMSMint.Core.Interfaces;
using SSMSMint.Core.Models;
using SSMSMint.Core.UI.Models;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace SSMSMint.Core.UI.ViewModels;

public class MixedLangCheckViewModel : INotifyPropertyChanged
{
    private IEnumerable<MixedLangWord> mixedLangWords;
    private readonly ITextDocumentManager tdManager;

    public MixedLangCheckViewModel(IEnumerable<MixedLangWord> mixedLangWords, ITextDocumentManager tdManager)
    {
        this.tdManager = tdManager;
        this.MixedLangWords = mixedLangWords;
    }

    public IEnumerable<MixedLangWord> MixedLangWords
    {
        get
        {
            return mixedLangWords;
        }
        set
        {
            mixedLangWords = value;
            OnPropertyChanged(nameof(MixedLangWords));
            OnPropertyChanged(nameof(MixedLangWordsCount));
        }
    }
    public int MixedLangWordsCount => MixedLangWords.Count();

    public event PropertyChangedEventHandler PropertyChanged;

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public async void MixedLangWordItemSelectionChanged(MixedLangWord word)
    {
        var sp = word.StartPoint;
        var ep = new TextPoint(sp.Line, sp.Column + word.Word.Length);
        await tdManager.SetSelectionAsync(new TextSpan(sp, ep));
    }
}

