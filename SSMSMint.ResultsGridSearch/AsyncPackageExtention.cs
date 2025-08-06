using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Shell;
using NLog;
using Task = System.Threading.Tasks.Task;

namespace SSMSMint.ResultsGridSearch
{
    public static class AsyncPackageExtention
    {
        public static async Task InitializeResultsGridSearch(this AsyncPackage package, ServiceCollection services)
        {
            services.AddSingleton<SearchProcessor>();
            services.AddTransient<ResultsGridSearchToolWindowViewModel>();

            await ResultsGridSearchToolWindowCommand.InitializeAsync(package);
            LogManager.GetCurrentClassLogger().Info($"{nameof(InitializeResultsGridSearch)} Initialized");
        }
    }
}
