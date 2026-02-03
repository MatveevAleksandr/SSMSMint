namespace SSMSMint.Core.Models;

public class SSMSMintSettings
    (
        bool locateInObjectExplorerEnabled,
        bool mixedLangInScriptWordsCheckEnabled,
        bool regionsEnabled,
        string regionStartKeyword,
        string regionEndKeyword,
        bool resultsGridSearchEnabled,
        bool scriptSqlObjectEnabled,
        bool includeNumberedProcedures,
        bool textMarkersEnabled
    )
{
    public bool LocateInObjectExplorerEnabled { get; } = locateInObjectExplorerEnabled;
    public bool MixedLangInScriptWordsCheckEnabled { get; } = mixedLangInScriptWordsCheckEnabled;
    public bool RegionsEnabled { get; } = regionsEnabled;
    public string RegionStartKeyword { get; } = regionStartKeyword;
    public string RegionEndKeyword { get; } = regionEndKeyword;
    public bool ResultsGridSearchEnabled { get; } = resultsGridSearchEnabled;
    public bool ScriptSqlObjectEnabled { get; } = scriptSqlObjectEnabled;
    public bool IncludeNumberedProcedures { get; } = includeNumberedProcedures;
    public bool TextMarkersEnabled { get; } = textMarkersEnabled;
}
