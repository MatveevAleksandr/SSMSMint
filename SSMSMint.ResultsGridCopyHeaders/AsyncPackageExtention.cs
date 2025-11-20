using Microsoft.VisualStudio.Shell;
using NLog;
using Task = System.Threading.Tasks.Task;

namespace SSMSMint.ResultsGridCopyHeaders;

public static class AsyncPackageExtention
{
    public static async Task InitializeResultsGridCopyHeaders(this AsyncPackage package)
    {
        await CopySelectedHeadersCommand.InitializeAsync(package);
        await CopyAllHeadersCommand.InitializeAsync(package);
        LogManager.GetCurrentClassLogger().Info($"{nameof(ResultsGridCopyHeaders)} Initialized");
    }
}