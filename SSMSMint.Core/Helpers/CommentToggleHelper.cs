using SSMSMint.Core.Models;
using System;
using System.IO;
using System.Text;

namespace SSMSMint.Core.Helpers;

public static class CommentToggleHelper
{
    private static string START_SELECTION_COMMENT_PREFIX => "/*";
    private static string END_SELECTION_COMMENT_POSTFIX => "*/";
    private static string LINE_COMMENT_PREFIX => "--";

    public static CommentType GetTextBlockCommentType(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return CommentType.None;
        }

        var trimmedText = text.Trim();

        if (trimmedText.StartsWith(START_SELECTION_COMMENT_PREFIX) && trimmedText.EndsWith(END_SELECTION_COMMENT_POSTFIX))
        {
            return CommentType.SurroundedComment;
        }

        var hasAnyNonEmptyLine = false;
        using var reader = new StringReader(text);
        string line;

        while ((line = reader.ReadLine()) != null)
        {
            var trimmedLine = line.Trim();
            if (string.IsNullOrEmpty(trimmedLine))
            {
                continue;
            }

            hasAnyNonEmptyLine = true;

            if (!trimmedLine.StartsWith(LINE_COMMENT_PREFIX))
            {
                return CommentType.None;
            }
        }

        if (hasAnyNonEmptyLine)
        {
            return CommentType.LineComment;
        }

        return CommentType.None;
    }

    public static string CommentText(string text, CommentType commentType)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        if (commentType == CommentType.SurroundedComment)
        {
            return string.Concat(START_SELECTION_COMMENT_PREFIX, text, END_SELECTION_COMMENT_POSTFIX);
        }
        else if (commentType == CommentType.LineComment)
        {
            var sb = new StringBuilder();
            using var reader = new StringReader(text);
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (sb.Length > 0)
                    sb.Append(Environment.NewLine);

                if (string.IsNullOrEmpty(line.Trim()))
                {
                    sb.Append(line);
                }
                else
                {
                    sb.Append(LINE_COMMENT_PREFIX).Append(line);
                }
            }
            return sb.ToString();
        }
        else
        {
            return text;
        }
    }

    public static string UncommentText(string text, CommentType commentType)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        if (commentType == CommentType.SurroundedComment)
        {
            int startIndex = text.IndexOf(START_SELECTION_COMMENT_PREFIX);
            int endIndex = text.LastIndexOf(END_SELECTION_COMMENT_POSTFIX);

            if (startIndex != -1 && endIndex > startIndex)
            {
                return text.Remove(endIndex, END_SELECTION_COMMENT_POSTFIX.Length).Remove(startIndex, START_SELECTION_COMMENT_PREFIX.Length);
            }
            else
            {
                return text;
            }
        }
        else if (commentType == CommentType.LineComment)
        {
            var sb = new StringBuilder();
            using var reader = new StringReader(text);
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (sb.Length > 0)
                    sb.Append(Environment.NewLine);

                var trimmedLine = line.TrimStart();
                if (trimmedLine.StartsWith(LINE_COMMENT_PREFIX))
                {
                    int commentMarkerIndex = line.IndexOf(LINE_COMMENT_PREFIX);
                    sb.Append(line.Remove(commentMarkerIndex, LINE_COMMENT_PREFIX.Length));
                }
                else
                {
                    sb.Append(line);
                }
            }
            return sb.ToString();
        }
        else
        {
            return text;
        }
    }
}
