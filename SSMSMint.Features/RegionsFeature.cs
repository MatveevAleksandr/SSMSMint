using SSMSMint.Core.Events;
using SSMSMint.Core.Interfaces;
using SSMSMint.Core.Models;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace SSMSMint.Features;

public class RegionsFeature(ISettingsManager settingsManager, EventBroker eventBroker) : IDisposable
{
    private readonly Logger logger = LogManager.GetCurrentClassLogger();

    public void Initialize()
    {
        
        eventBroker.WindowCreated += OnWindowCreatedAsync;
        logger.Info($"{nameof(RegionsFeature)} Initialized");
    }

    private async void OnWindowCreatedAsync(object sender, IWindowCreatedEventArgs w)
    {
        try
        {
            await CreateCustomRegionsAsync(w.TextDocumentManager);
        }
        catch (Exception ex)
        {
            logger.Error(ex);
        }
    }

    public async Task CreateCustomRegionsAsync(ITextDocumentManager tdManager)
    {
        var settings = settingsManager.GetSettings() ?? throw new Exception("Settings not found");
        if (!settings.RegionsEnabled)
        {
            return;
        }

        int lineIx = 0;
        var startRegionsPoints = new Stack<TextPoint>();
        var text = await tdManager.GetFullTextAsync();
        using var reader = new StringReader(text);
        string line;

        while ((line = reader.ReadLine()) != null)
        {
            lineIx++;

            var trimmedLine = line.Trim();
            var trimmedEndLine = line.TrimEnd();

            if (trimmedLine.StartsWith(settings.RegionStartKeyword))
            {
                startRegionsPoints.Push(new TextPoint(lineIx, trimmedEndLine.Length + 1)); // Начало региона фиксируем в конце строки
            }
            else if (trimmedLine.StartsWith(settings.RegionEndKeyword))
            {
                // Случай если окончаний региона больше, чем начал. Такое окончание проигнорируем
                if (startRegionsPoints.Count != 0)
                {
                    var sp = startRegionsPoints.Pop();
                    var ep = new TextPoint(lineIx, trimmedEndLine.Length + 1);
                    var span = new TextSpan(sp, ep);
                    await tdManager.OutlineSectionAsync(span);
                }
            }
        }
    }

    public void Dispose()
    {
        eventBroker.WindowCreated -= OnWindowCreatedAsync;
    }
}
