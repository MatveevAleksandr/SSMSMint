using EnvDTE;
using EnvDTE80;
using SSMSMint.Shared.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Shell;
using NLog;
using Task = System.Threading.Tasks.Task;

namespace SSMSMint.Shared.Extentions
{
    public static class AsyncPackageExtention
    {
        public static async Task InitializeSharedServices(this AsyncPackage package, ServiceCollection services)
        {
            var dte = (DTE2)await package.GetServiceAsync(typeof(DTE));

            services.AddSingleton<FrameService>();
            services.AddSingleton<DocumentEvents>(dte.Events.DocumentEvents);
            services.AddSingleton<WindowEvents>(dte.Events.WindowEvents);
            LogManager.GetCurrentClassLogger().Info($"{nameof(InitializeSharedServices)} Initialized");
        }
    }
}
