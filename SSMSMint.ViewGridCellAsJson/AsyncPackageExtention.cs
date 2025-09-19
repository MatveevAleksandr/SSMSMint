using Microsoft.VisualStudio.Shell;
using NLog;
using Task = System.Threading.Tasks.Task;

namespace SSMSMint.ViewGridCellAsJson;

public static class AsyncPackageExtention
{
    public static async Task InitializeViewGridCellAsJson(this AsyncPackage package)
    {
        await ViewGridCellAsJsonCommand.InitializeAsync(package);
        LogManager.GetCurrentClassLogger().Info($"{nameof(InitializeViewGridCellAsJson)} Initialized");
    }
}
