using Microsoft.SqlServer.Management.UI.VSIntegration.ObjectExplorer;
using System.Reflection;
using System.Windows.Forms;

namespace SSMSMint.Shared.Extentions
{
    public static class ObjectExplorerServiceExtentions
    {
        public static TreeView GetTreeView(this IObjectExplorerService service) => (TreeView)service.GetType().GetProperty("Tree", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(service);
    }
}

