using EnvDTE;
using Microsoft.VisualStudio.Shell;
using NLog;
using SSMSMint.Shared.Settings;
using System;
using Task = System.Threading.Tasks.Task;

namespace SSMSMint.Regions;

public static class AsyncPackageExtention
{
    private static AsyncPackage _package;

    public static async Task InitializeRegions(this AsyncPackage package, WindowEvents winEvents)
    {
        _package = package;
        var logger = LogManager.GetCurrentClassLogger();

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
            var settings = (SSMSMintSettings)_package.GetDialogPage(typeof(SSMSMintSettings)) ?? throw new Exception("Settings not found");
            var textDocument = (TextDocument)Window.Document.Object("TextDocument");
            textDocument.CreateCustomRegions(settings);
        }
        catch (Exception ex)
        {
            LogManager.GetCurrentClassLogger().Error(ex);
            throw;
        }
    }
}
