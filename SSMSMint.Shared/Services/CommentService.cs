using EnvDTE;

namespace SSMSMint.Shared.Services;

public static class CommentService
{
    private const string START_SELECTION_COMMENT_PREFIX = "/*";
    private const string END_SELECTION_COMMENT_POSTFIX = "*/";
    private const string LINE_COMMENT_PREFIX = "--";

    public enum CommentType
    {
        LineComment, // "--"
        SurroundedComment, // "/*...*/"
        None
    }

    /// <summary>
    /// Check whether this text is commented out
    /// </summary>
    /// <param name="text"></param>
    /// <param name="commentType"></param>
    /// <returns></returns>
    public static bool IsTextCommented(TextDocument doc, int lineStart, int colStart, int lineEnd, int colEnd, out CommentType commentType)
    {
        Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

        GetInnerPoints(doc, lineStart, colStart, lineEnd, colEnd, out EditPoint innerStart, out EditPoint innerEnd);

        // Make sure that the block is not highlighted /*...*/
        if (innerStart.GetText(START_SELECTION_COMMENT_PREFIX.Length) == START_SELECTION_COMMENT_PREFIX &&
            innerEnd.GetText(START_SELECTION_COMMENT_PREFIX.Length * -1) == END_SELECTION_COMMENT_POSTFIX)
        {
            commentType = CommentType.SurroundedComment;
            return true;
        }

        // Otherwise, let's check that the beginning of each line starts with --
        var p = innerStart.CreateEditPoint();
        var allLinesCommented = true;
        while (p.LessThan(innerEnd))
        {
            var innerP = SkipLeadingWs(p, innerEnd);
            if (innerP.GetText(LINE_COMMENT_PREFIX.Length) != LINE_COMMENT_PREFIX)
            {
                allLinesCommented = false;
                break;
            }
            p.LineDown();
            p.StartOfLine();
        }

        if (allLinesCommented)
        {
            commentType = CommentType.LineComment;
            return true;
        }

        commentType = CommentType.None;
        return false;
    }

    /// <summary>
    /// Comments on the text area depending on the specified comment type.
    /// </summary>
    /// <param name="doc"></param>
    /// <param name="lineStart"></param>
    /// <param name="colStart"></param>
    /// <param name="lineEnd"></param>
    /// <param name="colEnd"></param>
    /// <param name="commentType"></param>
    public static void CommentText(TextDocument doc, int lineStart, int colStart, int lineEnd, int colEnd, CommentType commentType)
    {
        Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
        try
        {
            doc.DTE.UndoContext.Open("Comment text"); // Opening the UndoContext so that the operation is rolled back with a single Undo

            if (commentType == CommentType.SurroundedComment)
            {
                var ep = doc.CreateEditPoint();
                ep.MoveToLineAndOffset(lineEnd, colEnd);
                ep.Insert(END_SELECTION_COMMENT_POSTFIX);
                ep.MoveToLineAndOffset(lineStart, colStart);
                ep.Insert(START_SELECTION_COMMENT_PREFIX);
            }
            else if (commentType == CommentType.LineComment)
            {
                var ep = doc.CreateEditPoint();
                for (int i = lineStart; i <= lineEnd; i++)
                {
                    ep.MoveToLineAndOffset(i, 1);
                    ep.Insert(LINE_COMMENT_PREFIX);
                }
            }
        }
        catch
        {
            throw;
        }
        finally
        {
            if (doc.DTE.UndoContext.IsOpen)
                doc.DTE.UndoContext.Close();
        }
    }


    /// <summary>
    /// Uncomment the text area
    /// </summary>
    /// <param name="doc"></param>
    /// <param name="lineStart"></param>
    /// <param name="colStart"></param>
    /// <param name="lineEnd"></param>
    /// <param name="colEnd"></param>
    public static void UncommentText(TextDocument doc, int lineStart, int colStart, int lineEnd, int colEnd)
    {
        Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
        try
        {
            doc.DTE.UndoContext.Open("Uncomment text"); // Opening the UndoContext so that the operation is rolled back with a single Undo

            GetInnerPoints(doc, lineStart, colStart, lineEnd, colEnd, out EditPoint innerStart, out EditPoint innerEnd);

            if (IsTextCommented(doc, lineStart, colStart, lineEnd, colEnd, out CommentType commentType) && commentType == CommentType.SurroundedComment)
            {
                innerStart.Delete(START_SELECTION_COMMENT_PREFIX.Length);
                innerEnd.Delete(END_SELECTION_COMMENT_POSTFIX.Length * -1);
                return;
            }

            var p = innerStart.CreateEditPoint();
            while (p.LessThan(innerEnd))
            {
                var innerP = SkipLeadingWs(p, innerEnd);
                if (innerP.GetText(LINE_COMMENT_PREFIX.Length) == LINE_COMMENT_PREFIX)
                {
                    innerP.Delete(LINE_COMMENT_PREFIX.Length);
                }
                p.LineDown();
                p.StartOfLine();
            }
        }
        catch
        {
            throw;
        }
        finally
        {
            if (doc.DTE.UndoContext.IsOpen)
                doc.DTE.UndoContext.Close();
        }
    }

    // Get an EditPoint from the text ignoring the spaces in front
    private static EditPoint SkipLeadingWs(EditPoint start, EditPoint end)
    {
        Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
        var p = start.CreateEditPoint();
        while (p.LessThan(end))
        {
            var s = p.GetText(1);
            if (s.Length == 0 || !char.IsWhiteSpace(s[0])) break;
            p.CharRight(1);
        }
        return p;
    }

    // Get an EditPoint from the text ignoring the spaces behind it
    private static EditPoint SkipTrailingWs(EditPoint end, EditPoint start)
    {
        Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
        var p = end.CreateEditPoint();
        while (p.GreaterThan(start))
        {
            p.CharLeft(1);
            var s = p.GetText(1);
            if (s.Length == 0 || !char.IsWhiteSpace(s[0]))
            {
                p.CharRight(1); // the position immediately after the last non-space character
                break;
            }
        }
        return p;
    }

    // Calculate the inner points of the beginning and end of a block of text without initial and final indents, spaces, line breaks, etc.
    private static void GetInnerPoints(TextDocument doc, int lineStart, int colStart, int lineEnd, int colEnd, out EditPoint innerStart, out EditPoint innerEnd)
    {
        Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
        var ep = doc.CreateEditPoint();
        EditPoint start;
        EditPoint end;

        // As such, there is no text selection, we will analyze the entire line.
        if (lineStart == lineEnd && colStart == colEnd)
        {
            ep.MoveToLineAndOffset(lineStart, 1);
            start = ep.CreateEditPoint();
            ep.EndOfLine();
            end = ep.CreateEditPoint();
        }
        else
        {
            ep.MoveToLineAndOffset(lineStart, colStart);
            start = ep.CreateEditPoint();
            ep.MoveToLineAndOffset(lineEnd, colEnd);
            end = ep.CreateEditPoint();
        }

        innerStart = SkipLeadingWs(start, end);
        innerEnd = SkipTrailingWs(end, start);
    }
}
