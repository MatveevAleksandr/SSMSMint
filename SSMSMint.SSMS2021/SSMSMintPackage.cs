using EnvDTE;
using EnvDTE80;
using SSMSMint.Core;
using SSMSMint.Core.Events;
using SSMSMint.Core.Extentions;
using SSMSMint.Core.Interfaces;
using SSMSMint.Core.Models;
using SSMSMint.Features;
using SSMSMint.SSMS2021.Commands;
using SSMSMint.SSMS2021.Implementations;
using SSMSMint.SSMS2021.Infrastructure;
using SSMSMint.SSMS2021.Settings;
using SSMSMint.SSMS2021.ToolWindows;
using Microsoft.SqlServer.Management.UI.VSIntegration.ObjectExplorer;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using NLog;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using Task = System.Threading.Tasks.Task;

namespace SSMSMint.SSMS2021;

[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
[Guid(PackageGuidString)]
// Говорим, что пакет должен быть загружен при старте. Дефолтное поведение - не грузим пока не понадобится как действие с UI (Например ждет нажатия кнопки)
[ProvideAutoLoad(VSConstants.UICONTEXT.NoSolution_string, PackageAutoLoadFlags.BackgroundLoad)]
[ProvideMenuResource("Menus.ctmenu", 1)]
[ProvideToolWindow(typeof(ResultsGridSearchToolWindow), Transient = true, MultiInstances = false)]
[ProvideToolWindow(typeof(MixedLangCheckToolWindow), Transient = true, MultiInstances = false)]
[ProvideOptionPage(typeof(SSMSMintSettingsPage), "SSMSMint", "General", 0, 0, true)]
public sealed class SSMSMintPackage : AsyncPackage
{
    private readonly string themeUriStr = "pack://application:,,,/SSMSMint.SSMS2021;component/Resources/MainSSMSTheme.xaml";
    public const string PackageGuidString = Consts.PackageGUIDstr;
    private DocumentEvents documentEvents;
    private WindowEvents windowEvents;
    private readonly EventBroker eventBroker = new();
    private readonly Dictionary<Window, VsTextLinesEventsListener> textLinesEvents = new();
    private Logger logger;

    private TextMarkerFeature textMarkerFeature;
    private RegionsFeature regionsFeature;


    protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
    {
        try
        {
            await base.InitializeAsync(cancellationToken, progress);
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            LogManager.Setup().LoadCustomConfiguration();
            logger = LogManager.GetCurrentClassLogger();

            var dte = (DTE2)await GetServiceAsync(typeof(DTE));

            IObjectExplorerService oeService = (IObjectExplorerService)await GetServiceAsync(typeof(IObjectExplorerService));

            // Нужно держать ссылки на Events иначе оно где то в процессе работы очищается даже если есть подписки
            windowEvents = dte.Events.WindowEvents;
            documentEvents = dte.Events.DocumentEvents;
            windowEvents.WindowCreated += OnSsmsWindowCreated;
            windowEvents.WindowClosing += OnSsmsWindowClosing;
            documentEvents.DocumentSaved += OnSsmsDocumentSaved;

            IUINotificationManager uINotificationManager = new UINotificationManagerImpl(this);
            ISettingsManager settingsManager = new SettingsManagerImpl(this);
            IWorkspaceManager workspaceManager = new WorkspaceManagerImpl(dte, this);
            IObjectExplorerManager objectExplorerManager = new ObjectExplorerManagerImpl(oeService);
            ISqlScriptProcessorManager sqlScriptProcessorManager = new SqlScriptProcessorManagerImpl(settingsManager);

            CommentToggleFeature commentToggleFeature = new();
            regionsFeature = new(settingsManager, eventBroker);
            CopyHeadersFeature copyHeadersFeature = new();
            ScriptSqlObjectFeature scriptSqlObjectFeature = new(workspaceManager, settingsManager, uINotificationManager, regionsFeature);
            ViewGridCellAsJsonFeature viewGridCellAsJsonFeature = new(workspaceManager);
            MixedLangInScriptWordsCheckFeature mixedLangInScriptWordsCheckFeature = new(workspaceManager, settingsManager, eventBroker, themeUriStr);
            ResultsGridSearchFeature resultsGridSearchFeature = new(workspaceManager);
            LocateInObjectExplorerFeature locateInObjectExplorerFeature = new(workspaceManager, uINotificationManager, objectExplorerManager);
            textMarkerFeature = new(settingsManager, eventBroker);

            regionsFeature.Initialize();
            textMarkerFeature.Initialize();
            mixedLangInScriptWordsCheckFeature.Initialize<MixedLangCheckToolWindow>();

            await CommentToggleCommand.InitializeAsync(this, commentToggleFeature, uINotificationManager);
            await RefreshRegionsCommand.InitializeAsync(this, regionsFeature, uINotificationManager, settingsManager);
            await ScriptSqlObjectCommand.InitializeAsync(this, uINotificationManager, sqlScriptProcessorManager, settingsManager, scriptSqlObjectFeature);
            await CopyAllHeadersCommand.InitializeAsync(this, copyHeadersFeature, workspaceManager, uINotificationManager);
            await CopySelectedHeadersCommand.InitializeAsync(this, copyHeadersFeature, workspaceManager, uINotificationManager);
            await ViewGridCellAsJsonCommand.InitializeAsync(this, viewGridCellAsJsonFeature, workspaceManager, uINotificationManager);
            await ResultsGridSearchCommand.InitializeAsync(this, uINotificationManager, workspaceManager, resultsGridSearchFeature, settingsManager, themeUriStr);
            await LocateInObjectExplorerCommand.InitializeAsync(this, uINotificationManager, settingsManager, locateInObjectExplorerFeature);

            logger.Info($"{nameof(SSMSMintPackage)} Initialized");
        }
        catch (Exception ex)
        {
            logger.Error(ex);
            throw;
        }
    }

    private void OnSsmsDocumentSaved(Document Document)
    {
        var tdManager = new TextDocumentManagerImpl((TextDocument)Document.Object("TextDocument"));
        var eventArgs = new DocumentSavedEventArgs(tdManager);
        eventBroker.RaiseDocumentSaved(eventArgs);
    }

    private void OnSsmsWindowCreated(Window window)
    {
        ITextMarkingManager tmManager = null;
        var winDoc = window.Document;
        var tdManager = new TextDocumentManagerImpl((TextDocument)winDoc.Object("TextDocument"));

        VsShellUtilities.IsDocumentOpen(ServiceProvider.GlobalProvider, winDoc.FullName, Guid.Empty, out _, out _, out var vsWindowFrame);
        if (vsWindowFrame != null)
        {
            vsWindowFrame.GetProperty((int)__VSFPROPID.VSFPROPID_DocData, out object lines);

            if (lines != null)
            {
                tmManager = new TextMarkingManagerImpl((IVsTextLines)lines);
                var textArgs = new EditorTextChangedEventArgs(tdManager, tmManager);
                // Подпишемся на прослушку изменения текста
                var listener = new VsTextLinesEventsListener(textArgs, (IVsTextLines)lines, eventBroker);
                if (!textLinesEvents.ContainsKey(window))
                    textLinesEvents.Add(window, listener);
            }
        }

        var winArgs = new WindowCreatedEventArgs(tdManager, tmManager);
        eventBroker.RaiseWindowCreated(winArgs);
    }

    private void OnSsmsWindowClosing(Window Window)
    {
        if (textLinesEvents.TryGetValue(Window, out var listener))
        {
            listener.Dispose();
            textLinesEvents.Remove(Window);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (windowEvents != null)
            {
                windowEvents.WindowCreated -= OnSsmsWindowCreated;
                windowEvents.WindowCreated -= OnSsmsWindowClosing;
            }
            if (documentEvents != null)
            {
                documentEvents.DocumentSaved -= OnSsmsDocumentSaved;
            }

            regionsFeature?.Dispose();
            textMarkerFeature?.Dispose();
        }
        base.Dispose(disposing);
    }
}