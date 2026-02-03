using SSMSMint.Core.Interfaces;
using SSMSMint.Core.UI.Interfaces;

namespace SSMSMint.Core.UI.Models;

public class ResultsGridsSearchToolWindowParams(IResultsGridSearchFeature feature, IUINotificationManager uINotificationManager, string themeUriStr) : IToolWindowParams
{
    public IResultsGridSearchFeature Feature => feature;
    public IUINotificationManager UINotificationManager => uINotificationManager;
    public string ThemeUriStr => themeUriStr;
}
