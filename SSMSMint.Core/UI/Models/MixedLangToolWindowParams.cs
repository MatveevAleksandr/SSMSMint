using SSMSMint.Core.Interfaces;
using SSMSMint.Core.UI.Interfaces;
using System.Collections.Generic;

namespace SSMSMint.Core.UI.Models;

public class MixedLangToolWindowParams(IEnumerable<MixedLangWord> mixedLangWords, ITextDocumentManager tdManager, string themeUriStr) : IToolWindowParams
{
    public readonly IEnumerable<MixedLangWord> MixedLangWords = mixedLangWords;
    public readonly ITextDocumentManager TdManager = tdManager;

    public string ThemeUriStr => themeUriStr;
}
