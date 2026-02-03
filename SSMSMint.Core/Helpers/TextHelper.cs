using SSMSMint.Core.Models;
using System;

namespace SSMSMint.Core.Helpers;

public static class TextHelper
{
    public static TextPoint GetPosition(string text, int index)
    {
        if (index < 0 || index > text.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        if (text.Length == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(text));
        }

        int lineNumber = 1;
        int lastNewlineIndex = -1;

        for (int i = 0; i < index; i++)
        {
            if (text[i] == '\n')
            {
                lineNumber++;
                lastNewlineIndex = i;
            }
        }

        return new TextPoint(lineNumber, index - lastNewlineIndex);
    }
}
