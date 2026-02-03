using SSMSMint.Core.Interfaces;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace SSMSMint.SSMS2022.Implementations;

internal class UINotificationManagerImpl(AsyncPackage package) : IUINotificationManager
{
    public void ShowError(string title, string msg)
    {
        VsShellUtilities.ShowMessageBox(
                package,
                msg,
                title,
                OLEMSGICON.OLEMSGICON_CRITICAL,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
    }

    public void ShowInfo(string title, string msg)
    {
        VsShellUtilities.ShowMessageBox(
                package,
                msg,
                title,
                OLEMSGICON.OLEMSGICON_INFO,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
    }

    public void ShowWarning(string title, string msg)
    {
        VsShellUtilities.ShowMessageBox(
                package,
                msg,
                title,
                OLEMSGICON.OLEMSGICON_WARNING,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
    }
}
