using SSMSMint.Core.Events;
using SSMSMint.Core.Interfaces;
using SSMSMint.Core.Models;
using SSMSMint.Core.Visitors;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SSMSMint.Features;

public class TextMarkerFeature : IDisposable
{
    private readonly Logger logger = LogManager.GetCurrentClassLogger();
    private readonly Timer textChangingTimer;
    private readonly object timerLock = new();
    private readonly object argsLock = new();
    private readonly ISettingsManager settingsManager;
    private readonly EventBroker eventBroker;
    private ITextDocumentManager lastTextDocumentManager;
    private ITextMarkingManager lastTextMarkingManager;

    public TextMarkerFeature(ISettingsManager settingsManager, EventBroker eventBroker)
    {
        this.settingsManager = settingsManager;
        this.eventBroker = eventBroker;
        textChangingTimer = new(async _ => await ProcessMarkersAsync(), null, Timeout.Infinite, Timeout.Infinite);
    }

    public void Initialize()
    {
        eventBroker.EditorTextChanged += OnEditorTextChanged;
        eventBroker.WindowCreated += OnWindowCreated;
        logger.Info($"{nameof(TextMarkerFeature)} Initialized");
    }

    private async void OnWindowCreated(object sender, IWindowCreatedEventArgs e)
    {
        // Запомним аргументы последнего окна в котором менялся текст
        lock (argsLock)
        {
            lastTextDocumentManager = e.TextDocumentManager;
            lastTextMarkingManager = e.TextMarkingManager;
        }
        try
        {
            await ProcessMarkersAsync();
        }
        catch (Exception ex)
        {
            logger.Error(ex.Message);
        }
    }

    private void OnEditorTextChanged(object sender, IEditorTextChangedEventArgs e)
    {
        // Запомним аргументы последнего окна в котором менялся текст
        lock (argsLock)
        {
            lastTextDocumentManager = e.TextDocumentManager;
            lastTextMarkingManager = e.TextMarkingManager;
        }

        // При каждом изменении текста дадим небольшой лаг, чтобы не прерывать печатание
        lock (timerLock)
        {
            textChangingTimer.Change(1000, Timeout.Infinite);
        }
    }

    private async Task ProcessMarkersAsync()
    {
        ITextDocumentManager tdManager;
        ITextMarkingManager tmManager;
        lock (argsLock)
        {
            tdManager = lastTextDocumentManager;
            tmManager = lastTextMarkingManager;
        }

        if (tdManager == null || tmManager == null)
            return;

        var settings = settingsManager.GetSettings() ?? throw new Exception("Settings not found");
        if (!settings.TextMarkersEnabled)
            return;

        var parser = new TSql150Parser(true);

        tmManager.ClearAllMarkers();

        var scriptText = await tdManager.GetFullTextAsync();
        var parsedSqlScript = (TSqlScript)parser.Parse(new StringReader(scriptText), out var _);

        if (parsedSqlScript is null)
            return;

        var allNotUsedVarsSpans = new List<TextSpan>();
        var allNotDeclaredVarsSpans = new List<TextSpan>();

        foreach (var batch in parsedSqlScript.Batches)
        {
            var visitor = new TextMarkerVisitor();
            batch.Accept(visitor);

            allNotUsedVarsSpans.AddRange(visitor.NotUsedVars);
            allNotDeclaredVarsSpans.AddRange(visitor.NotDeclaredVars);
        }

        var notUsedVarsGroup = new MarkersGroupDefinition(MarkerKind.NotUsedVars, "Переменная объявлена, но не используется", allNotUsedVarsSpans);
        var notDeclaredVarsGroup = new MarkersGroupDefinition(MarkerKind.NotDeclaredVars, "Переменная не объявлена", allNotDeclaredVarsSpans);

        tmManager.ApplyMarkers(notUsedVarsGroup);
        tmManager.ApplyMarkers(notDeclaredVarsGroup);
    }

    public void Dispose()
    {
        eventBroker.EditorTextChanged -= OnEditorTextChanged;
        textChangingTimer?.Dispose();
    }
}
