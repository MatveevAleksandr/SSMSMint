using Microsoft.VisualStudio.Shell;
using NLog;
using Task = System.Threading.Tasks.Task;

namespace SSMSMint.ResultsGridSearch;

public static class AsyncPackageExtention
{
    public static async Task InitializeResultsGridSearch(this AsyncPackage package)
    {
        await ResultsGridSearchToolWindowCommand.InitializeAsync(package);
        LogManager.GetCurrentClassLogger().Info($"{nameof(InitializeResultsGridSearch)} Initialized");
    }
}
