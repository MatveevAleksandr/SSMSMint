using EnvDTE;
using SSMSMint.Shared.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Shell;
using NLog;
using System;
using Task = System.Threading.Tasks.Task;

namespace SSMSMint.Regions
{
    public static class AsyncPackageExtention
    {
        public static async Task InitializeRegions(this AsyncPackage package)
        {
            var logger = LogManager.GetCurrentClassLogger();
            var winEvents = ServicesLocator.ServiceProvider.GetRequiredService<WindowEvents>();

            await RefreshRegionsCommand.InitializeAsync(package);

            if (winEvents == null)
            {
                logger.Info($"{nameof(InitializeRegions)} not Initialized. Registered window events not found");
                return;
            }
            winEvents.WindowCreated += WinEvents_WindowCreated;

            logger.Info($"{nameof(InitializeRegions)} Initialized");
        }

        private static void WinEvents_WindowCreated(Window Window)
        {
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                var textDocument = (TextDocument)Window.Document.Object("TextDocument");
                textDocument.CreateCustomRegions();
            }
            catch (Exception ex)
            {
                LogManager.GetCurrentClassLogger().Error(ex);
                throw;
            }
        }
    }
}
