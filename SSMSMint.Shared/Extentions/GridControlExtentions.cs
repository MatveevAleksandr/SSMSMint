using Microsoft.SqlServer.Management.UI.Grid;
using System.Reflection;

namespace SSMSMint.Shared.Extentions
{
    public static class GridControlExtentions
    {
        public static SelectionManager GetSelectionManager(this IGridControl grid) => grid.GetType().GetField("m_selMgr", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(grid) as SelectionManager;
        public static MethodInfo GetGridOnSelectionChanged(this IGridControl grid) => grid.GetType().GetMethod("OnSelectionChanged", BindingFlags.NonPublic | BindingFlags.Instance);
    }
}
