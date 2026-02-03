namespace SSMSMint.Core.Models;

public readonly struct TextPoint(int line, int column)
{
    public readonly int Line { get; } = line;
    public readonly int Column { get; } = column;
}
