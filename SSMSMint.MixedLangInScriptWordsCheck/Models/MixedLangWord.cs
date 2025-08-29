namespace SSMSMint.MixedLangInScriptWordsCheck.Models;

internal class MixedLangWord(int lineIndex, int columnIndex, string word)
{
    public int LineIndex { get; set; } = lineIndex;
    public int ColumnIndex { get; set; } = columnIndex;
    public string Word { get; set; } = word;
}