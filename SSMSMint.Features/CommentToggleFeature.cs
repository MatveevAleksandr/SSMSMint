using SSMSMint.Core.Helpers;
using SSMSMint.Core.Interfaces;
using SSMSMint.Core.Models;
using System.Threading.Tasks;

namespace SSMSMint.Features;

public class CommentToggleFeature()
{
    public async Task ToggleComment(ITextDocumentManager tdManager)
    {
        string res;
        TextSpan span;
        var selectionSpan = await tdManager.GetSelectionAsync();
        // Если ничего не выделено, то будем анализировать строку целиком
        if (selectionSpan.IsEmpty)
        {
            var nsp = new TextPoint(selectionSpan.Start.Line, 1);
            var nep = new TextPoint(selectionSpan.Start.Line, await tdManager.GetColumnCountAsync(selectionSpan.Start.Line));
            span = new TextSpan(nsp, nep);
        }
        else
        {
            span = selectionSpan;
        }

        var selectionText = await tdManager.GetTextAsync(span);
        var commType = CommentToggleHelper.GetTextBlockCommentType(selectionText);

        if (commType != CommentType.None)
        {
            res = CommentToggleHelper.UncommentText(selectionText, commType);
        }
        else
        {
            if (selectionSpan.IsEmpty)
                res = CommentToggleHelper.CommentText(selectionText, CommentType.LineComment);
            else
                res = CommentToggleHelper.CommentText(selectionText, CommentType.SurroundedComment);
        }

        await tdManager.ReplaceTextAsync(span, res);

        if (!selectionSpan.IsEmpty)
        {
            var nsel = await tdManager.GetSelectionAsync();
            var nsp = new TextSpan(selectionSpan.Start, nsel.End);
            await tdManager.SetSelectionAsync(nsp);
        }
    }
}
