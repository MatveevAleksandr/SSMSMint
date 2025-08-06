using Microsoft.VisualStudio.Shell;
using NLog;
using Task = System.Threading.Tasks.Task;

namespace SSMSMint.LocateInObjectExplorer
{
    public static class AsyncPackageExtention
    {
        public static async Task InitializeLocateInObjectExplorer(this AsyncPackage package)
        {
            await LocateInObjectExplorerCommand.InitializeAsync(package);
            LogManager.GetCurrentClassLogger().Info($"{nameof(InitializeLocateInObjectExplorer)} Initialized");
        }
    }
}
