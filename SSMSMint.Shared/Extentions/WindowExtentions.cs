using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;

namespace SSMSMint.Shared.Extentions;

public static class WindowExtentions
{
    public static void GetWindowFrame(this Window window, out IVsWindowFrame vsWindowFrame) => VsShellUtilities.IsDocumentOpen(ServiceProvider.GlobalProvider, window.Document.FullName, Guid.Empty, out _, out _, out vsWindowFrame);
}
