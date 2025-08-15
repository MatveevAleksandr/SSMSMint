using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using NLog;
using SSMSMint.LocateInObjectExplorer;
using SSMSMint.MixedLangInScriptWordsCheck;
using SSMSMint.Regions;
using SSMSMint.ResultsGridSearch;
using SSMSMint.ScriptSqlObject;
using SSMSMint.Shared.Extentions;
using SSMSMint.Shared.Services;
using SSMSMint.Shared.Settings;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using Task = System.Threading.Tasks.Task;

namespace SSMSMint.VSIX
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(PackageGuidString)]
    // Говорим, что пакет должен быть загружен при старте. Дефолтное поведение - не грузим пока не понадобится как действие с UI (Например ждет нажатия кнопки)
    [ProvideAutoLoad(VSConstants.UICONTEXT.NoSolution_string, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideOptionPage(typeof(SSMSMintSettings), "SSMSMint", "General", 0, 0, true)]
    [ProvideToolWindow(typeof(ResultsGridSearchToolWindow), Transient = true, MultiInstances = false)]
    public sealed class SSMSMintPackage : AsyncPackage
    {
        public const string PackageGuidString = "94de8f9c-19ee-4f89-a394-81b5b17c0e0e";

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await base.InitializeAsync(cancellationToken, progress);
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            try
            {
                LogManager.Setup().LoadCustomConfiguration();
                var services = new ServiceCollection();

                await this.InitializeSharedServices(services);
                await this.InitializeResultsGridSearch(services);
                ServicesLocator.ServiceProvider = services.BuildServiceProvider();
                await this.InitializeLocateInObjectExplorer();
                await this.InitializeScriptSqlObject();
                await this.InitializeRegions();
                this.InitializeMixedLangInScriptWordsCheck();

                LogManager.GetCurrentClassLogger().Info("Initialized");
            }
            catch (Exception ex)
            {
                LogManager.GetCurrentClassLogger().Error(ex);
                throw;
            }
        }
    }
}
