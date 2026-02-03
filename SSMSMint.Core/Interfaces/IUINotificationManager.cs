namespace SSMSMint.Core.Interfaces;

public interface IUINotificationManager
{
    void ShowError(string title, string msg);
    void ShowWarning(string title, string msg);
    void ShowInfo(string title, string msg);
}
