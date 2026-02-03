using SSMSMint.Core.Events;
using SSMSMint.Core.Helpers;
using SSMSMint.Core.Interfaces;
using SSMSMint.Core.UI.Interfaces;
using SSMSMint.Core.UI.Models;
using NLog;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SSMSMint.Features;

public class MixedLangInScriptWordsCheckFeature(
    IWorkspaceManager workspaceManager,
    ISettingsManager settingsManager,
    EventBroker eventBroker,
    string themeUriStr
    )
{
    private readonly Logger logger = LogManager.GetCurrentClassLogger();

    public void Initialize<T>() where T : IToolWindowCore
    {
        eventBroker.DocumentSaved += OnDocumentSavedAsync<T>;
        logger.Info($"{nameof(MixedLangInScriptWordsCheckFeature)} Initialized");
    }

    private async void OnDocumentSavedAsync<T>(object sender, IDocumentSavedEventArgs e) where T : IToolWindowCore
    {
        try
        {
            var settings = settingsManager.GetSettings() ?? throw new Exception("Settings not found");
            if (!settings.MixedLangInScriptWordsCheckEnabled)
            {
                return;
            }

            var tdManager = e.TextDocumentManager;
            var scriptText = await tdManager.GetFullTextAsync();
            var regex = new Regex(@"(?=[а-яА-ЯёЁ]*[a-zA-Z])(?=[a-zA-Z]*[а-яА-ЯёЁ])[а-яА-ЯёЁa-zA-Z]+", RegexOptions.Compiled);
            var matches = regex.Matches(scriptText);

            var mixedLangWords = new List<MixedLangWord>();
            foreach (Match match in matches)
            {
                var pos = TextHelper.GetPosition(scriptText, match.Index);
                var word = new MixedLangWord(pos, match.Value);
                mixedLangWords.Add(word);
            }

            var twParams = new MixedLangToolWindowParams(mixedLangWords, tdManager, themeUriStr);

            if (mixedLangWords.Count != 0)
            {
                await workspaceManager.ShowToolWindowAsync<T>(twParams);
            }
            else
            {
                await workspaceManager.CloseToolWindowAsync<T>();
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex);
        }
    }
}
