using EnvDTE;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using NLog;
using SSMSMint.Shared.Services;
using SSMSMint.Shared.Settings;
using System;
using System.Text.RegularExpressions;

namespace SSMSMint.MixedLangInScriptWordsCheck
{
    public static class AsyncPackageExtention
    {
        private static AsyncPackage _package;

        public static void InitializeMixedLangInScriptWordsCheck(this AsyncPackage package)
        {
            _package = package;
            var logger = LogManager.GetCurrentClassLogger();
            var docEvents = ServicesLocator.ServiceProvider.GetRequiredService<DocumentEvents>();
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

                var vsTextView = VsShellUtilities.IsDocumentOpen(Microsoft.VisualStudio.Shell.ServiceProvider.GlobalProvider, document.FullName, Guid.Empty, out _, out _, out IVsWindowFrame windowFrame)
                    ? VsShellUtilities.GetTextView(windowFrame)
                    : null;

                if (vsTextView == null)
                {
                    logger.Error($"{nameof(vsTextView)} is null");
                    return;
                }

                var result = vsTextView.GetBuffer(out var textLines);
                textLines.GetLastLineIndex(out var lastLine, out var lastIndex);
                vsTextView.GetTextStream(0, 0, lastLine, lastIndex, out var scriptText);

                var regex = new Regex(@"\b(?=\w*[а-яА-Я])(?=\w*[a-zA-Z])\w+\b", RegexOptions.Compiled);
                var matches = regex.Matches(scriptText);

                if (matches.Count == 0)
                {
                    return;
                }

                var words = "Обнаружены слова, содержащие как русские, так и английские буквы:\n\n";
                foreach (Match match in matches)
                {
                    vsTextView.GetLineAndColumn(match.Index, out var line, out var _);
                    words += $"Line - {line + 1}: {match.Value}\n";
                }

                VsShellUtilities.ShowMessageBox(
                    _package,
                    words,
                    "Смешанный язык в словах",
                    OLEMSGICON.OLEMSGICON_INFO,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                throw;
            }
        }
    }
}
