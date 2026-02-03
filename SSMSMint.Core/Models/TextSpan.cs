namespace SSMSMint.Core.Models;

public readonly struct TextSpan(TextPoint start, TextPoint end)
{
    public readonly TextPoint Start = start;
    public readonly TextPoint End = end;
    public readonly bool IsEmpty => Start.Line == End.Line && Start.Column == End.Column;
}
