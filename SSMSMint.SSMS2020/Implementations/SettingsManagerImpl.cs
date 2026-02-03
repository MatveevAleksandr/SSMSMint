using SSMSMint.Core.Interfaces;
using SSMSMint.Core.Models;
using SSMSMint.SSMS2020.Settings;
using Microsoft.VisualStudio.Shell;
using System;

namespace SSMSMint.SSMS2020.Implementations;

internal class SettingsManagerImpl(AsyncPackage package) : ISettingsManager
{
    public SSMSMintSettings GetSettings()
    {
        var settingsPage = (SSMSMintSettingsPage)package.GetDialogPage(typeof(SSMSMintSettingsPage)) ?? throw new Exception("Settings not found");
        return new SSMSMintSettings(
                settingsPage.LocateInObjectExplorerEnabled,
                settingsPage.MixedLangInScriptWordsCheckEnabled,
                settingsPage.RegionsEnabled,
                settingsPage.RegionStartKeyword,
                settingsPage.RegionEndKeyword,
                settingsPage.ResultsGridSearchEnabled,
                settingsPage.ScriptSqlObjectEnabled,
                settingsPage.IncludeNumberedProcedures,
                settingsPage.TextMarkersEnabled
            );
    }
}
