using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using NLog;
using SSMSMint.LocateInObjectExplorer;
using SSMSMint.MixedLangInScriptWordsCheck;
using SSMSMint.Regions;
using SSMSMint.ResultsGridSearch;
using SSMSMint.ScriptSqlObject;
using SSMSMint.Shared.Extentions;
using SSMSMint.Shared.Settings;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using Task = System.Threading.Tasks.Task;

namespace SSMSMint.VSIX;

[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
[Guid(PackageGuidString)]
// Говорим, что пакет должен быть загружен при старте. Дефолтное поведение - не грузим пока не понадобится как действие с UI (Например ждет нажатия кнопки)
[ProvideAutoLoad(VSConstants.UICONTEXT.NoSolution_string, PackageAutoLoadFlags.BackgroundLoad)]
[ProvideMenuResource("Menus.ctmenu", 1)]
[ProvideOptionPage(typeof(SSMSMintSettings), "SSMSMint", "General", 0, 0, true)]
[ProvideToolWindow(typeof(ResultsGridSearchToolWindow), Transient = true, MultiInstances = false)]
[ProvideToolWindow(typeof(MixedLangCheckToolWindow), Transient = true, MultiInstances = false)]
public sealed class SSMSMintPackage : AsyncPackage
{
    public const string PackageGuidString = "94de8f9c-19ee-4f89-a394-81b5b17c0e0e";
    public DocumentEvents DocumentEvents { get; set; }
    public WindowEvents WindowEvents { get; set; }

    protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
    {
        await base.InitializeAsync(cancellationToken, progress);
        await JoinableTaskFactory.SwitchToMainThreadAsync();

        try
        {
            LogManager.Setup().LoadCustomConfiguration();

            // Нужно держать ссылки на Events иначе оно где то в процессе работы очищается даже если есть подписки
            var dte = (DTE2)await GetServiceAsync(typeof(DTE));
            DocumentEvents = dte.Events.DocumentEvents;
            WindowEvents = dte.Events.WindowEvents;

            await this.InitializeResultsGridSearch();
            await this.InitializeLocateInObjectExplorer();
            await this.InitializeScriptSqlObject();
            await this.InitializeRegions(WindowEvents);
            this.InitializeMixedLangInScriptWordsCheck(DocumentEvents);

            LogManager.GetCurrentClassLogger().Info("Initialized");
        }
        catch (Exception ex)
        {
            LogManager.GetCurrentClassLogger().Error(ex);
            throw;
        }
    }
}
