using Microsoft.VisualStudio.Shell;
using NLog;
using Task = System.Threading.Tasks.Task;

namespace SSMSMint.CommentToggle;

public static class AsyncPackageExtention
{
    public static async Task InitializeCommentToggle(this AsyncPackage package)
    {
        await CommentToggleCommand.InitializeAsync(package);
        LogManager.GetCurrentClassLogger().Info($"{nameof(InitializeCommentToggle)} Initialized");
    }
}
