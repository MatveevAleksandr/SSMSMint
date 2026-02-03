using System.Collections.Generic;

namespace SSMSMint.Core.Models;

public class MarkersGroupDefinition(MarkerKind kind, string tooltip, IEnumerable<TextSpan> spans)
{
    public MarkerKind Kind { get; } = kind;
    public string ToolTip { get; } = tooltip;
    public IEnumerable<TextSpan> Spans { get; } = spans;
}
