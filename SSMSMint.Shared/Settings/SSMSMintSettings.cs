using Microsoft.VisualStudio.Shell;
using System.ComponentModel;

namespace SSMSMint.Shared.Settings;

public class SSMSMintSettings : DialogPage
{
    [Category("Locate in object explorer")]
    [DisplayName("Enabled")]
    [Description("Enable the locate in object explorer feature")]
    public bool LocateInObjectExplorerEnabled { get; set; } = true;

    [Category("Mixed language in script")]
    [DisplayName("Enabled")]
    [Description("Enable the mixed language in script check feature")]
    public bool MixedLangInScriptWordsCheckEnabled { get; set; } = true;

    [Category("Regions")]
    [DisplayName("Enabled")]
    [Description("Enable the regions feature")]
    public bool RegionsEnabled { get; set; } = true;

    [Category("Regions")]
    [DisplayName("Region start keyword")]
    [Description("The keyword after which the name and region will begin")]
    public string RegionStartKeyword { get; set; } = "--#region";

    [Category("Regions")]
    [DisplayName("Region end keyword")]
    [Description("The keyword after which the region will end")]
    public string RegionEndKeyword { get; set; } = "--#endregion";

    [Category("Results grid search")]
    [DisplayName("Enabled")]
    [Description("Enable the results grid search feature")]
    public bool ResultsGridSearchEnabled { get; set; } = true;

    [Category("Script sql object")]
    [DisplayName("Enabled")]
    [Description("Enable the script sql object feature")]
    public bool ScriptSqlObjectEnabled { get; set; } = true;

    [Category("Script sql object")]
    [DisplayName("Include numbered procedures")]
    [Description("When generating a script for a regular procedure, all numbered versions of it will be automatically scripted")]
    public bool IncludeNumberedProcedures { get; set; } = true;

    [Category("Text markers")]
    [DisplayName("Enabled")]
    [Description("Enable the creation of text markers (wavy lines under words). You need to reopen the tabs")]
    public bool TextMarkersEnabled { get; set; } = true;
}
