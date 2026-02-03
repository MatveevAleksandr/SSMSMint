using SSMSMint.Core.Models;

namespace SSMSMint.Core.UI.Models;

public class MixedLangWord(TextPoint startPoint, string word)
{
    public TextPoint StartPoint { get; set; } = startPoint;
    public string Word { get; set; } = word;
}