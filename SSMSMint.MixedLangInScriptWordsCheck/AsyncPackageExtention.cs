using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using NLog;
using SSMSMint.MixedLangInScriptWordsCheck.Models;
using SSMSMint.MixedLangInScriptWordsCheck.Views;
using SSMSMint.MixedLangInScriptWordsCheck.ViewModels;
using SSMSMint.Shared.Settings;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SSMSMint.MixedLangInScriptWordsCheck;

public static class AsyncPackageExtention
{
    private static AsyncPackage _package;

    public static void InitializeMixedLangInScriptWordsCheck(this AsyncPackage package, DocumentEvents docEvents)
    {
        _package = package;
        var logger = LogManager.GetCurrentClassLogger();
        if (docEvents == null)
        {
            logger.Info($"{nameof(InitializeMixedLangInScriptWordsCheck)} not Initialized. Registered document events not found");
            return;
        }
        docEvents.DocumentSaved += DocEvents_DocumentSaved;
        logger.Info($"{nameof(InitializeMixedLangInScriptWordsCheck)} Initialized");
    }

    private static void DocEvents_DocumentSaved(Document document)
    {
        var logger = LogManager.GetCurrentClassLogger();
        try
        {
            var settings = (SSMSMintSettings)_package.GetDialogPage(typeof(SSMSMintSettings)) ?? throw new Exception("Settings not found");
            if (!settings.MixedLangInScriptWordsCheckEnabled)
            {
                return;
            }

            var vsTextView = VsShellUtilities.IsDocumentOpen(ServiceProvider.GlobalProvider, document.FullName, Guid.Empty, out _, out _, out IVsWindowFrame windowFrame)
                ? VsShellUtilities.GetTextView(windowFrame)
                : null;

            if (vsTextView == null)
            {
                logger.Error($"{nameof(vsTextView)} is null");
                return;
            }

            ToolWindowPane toolWindow = _package.FindToolWindow(typeof(MixedLangCheckToolWindow), 0, true);
            if ((null == toolWindow) || (null == toolWindow.Frame))
            {
                logger.Error($"{nameof(MixedLangCheckToolWindow)} Cannot create tool window");
                return;
            }
            var control = (MixedLangCheckToolWindowControl)toolWindow.Content;
            var vm = (MixedLangCheckToolWindowViewModel)control.DataContext;
            var toolWindowFrame = (IVsWindowFrame)toolWindow.Frame;

            var result = vsTextView.GetBuffer(out var textLines);
            textLines.GetLastLineIndex(out var lastLine, out var lastIndex);
            vsTextView.GetTextStream(0, 0, lastLine, lastIndex, out var scriptText);

            var regex = new Regex(@"(?=[а-яА-ЯёЁ]*[a-zA-Z])(?=[a-zA-Z]*[а-яА-ЯёЁ])[а-яА-ЯёЁa-zA-Z]+", RegexOptions.Compiled);
            var matches = regex.Matches(scriptText);

            var mixedLangWords = new List<MixedLangWord>();
            foreach (Match match in matches)
            {
                vsTextView.GetLineAndColumn(match.Index, out var line, out var column);
                var word = new MixedLangWord(line, column, match.Value);
                mixedLangWords.Add(word);
            }

            vm.InitParams(mixedLangWords, _package);

            if (mixedLangWords.Count != 0)
            {
                Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(toolWindowFrame.Show());
            }
            else
            {
                toolWindowFrame.CloseFrame((uint)__FRAMECLOSE.FRAMECLOSE_NoSave);
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex);
            throw;
        }
    }
}