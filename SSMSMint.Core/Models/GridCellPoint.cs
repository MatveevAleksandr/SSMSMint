namespace SSMSMint.Core.Models;

public readonly struct GridCellPoint(long row, int column)
{
    public readonly long Row { get; } = row;
    public readonly int Column { get; } = column;
}
