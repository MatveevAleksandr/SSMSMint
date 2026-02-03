using SSMSMint.Core.Models;
using System.Threading.Tasks;

namespace SSMSMint.Core.Interfaces;

public interface ITextDocumentManager
{
    public Task<string> GetTextAsync(TextSpan ts);
    public Task<string> GetFullTextAsync();
    public Task<TextSpan> GetSelectionAsync();
    public Task SetSelectionAsync(TextSpan ts);
    public Task OutlineSectionAsync(TextSpan ts);
    public Task<int> GetColumnCountAsync(int line);
    public Task ReplaceTextAsync(TextSpan textSpan, string newText);
    public Task<TextPoint> GetCaretPositionAsync();
}
