using Microsoft.VisualStudio.Shell;
using NLog;
using Task = System.Threading.Tasks.Task;

namespace SSMSMint.ScriptSqlObject;

public static class AsyncPackageExtention
{
    public static async Task InitializeScriptSqlObject(this AsyncPackage package)
    {
        await ScriptSqlObjectAtCursorCommand.InitializeAsync(package);
        LogManager.GetCurrentClassLogger().Info($"{nameof(InitializeScriptSqlObject)} Initialized");
    }
}
