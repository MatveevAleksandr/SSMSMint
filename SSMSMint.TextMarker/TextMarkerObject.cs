namespace SSMSMint.TextMarker;

internal class TextMarkerObject(int startLine, int startIndex, int endLine, int endIndex)
{
    public int StartLine { get; set; } = startLine;
    public int StartIndex { get; set; } = startIndex;
    public int EndLine { get; set; } = endLine;
    public int EndIndex { get; set; } = endIndex;
}