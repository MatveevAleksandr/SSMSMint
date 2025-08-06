using Microsoft.SqlServer.Management.UI.VSIntegration.ObjectExplorer;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SSMSMint.Shared.Extentions
{
    public static class HierarchyTreeNodeExtentions
    {
        public static async Task WaitForExpansion(this HierarchyTreeNode node) => await WaitForExpansion(node, 100, 300);

        public static async Task WaitForExpansion(this HierarchyTreeNode node, int maxAttempts, int delayMs)
        {
            int attempts = 0;

            while (attempts < maxAttempts && node.Expanding)
            {
                await Task.Delay(delayMs);
                attempts++;
            }
        }

        public static HierarchyTreeNode FindNode(this TreeNodeCollection nodes, string searchNodeText)
        {
            if (nodes == null)
            {
                return null;
            }

            foreach (HierarchyTreeNode node in nodes)
            {
                if (node.Text.ToLower() == searchNodeText.ToLower() ||
                    node.Text.ToLower().StartsWith($"{searchNodeText.ToLower()} "))
                {
                    return node;
                }
            }

            return null;
        }

        public static async Task<HierarchyTreeNode> FindAndExpandNode(this HierarchyTreeNode rootNode, string searchNodeText)
        {
            if (rootNode == null)
            {
                return null;
            }

            var node = rootNode.Nodes.FindNode(searchNodeText);
            if (node == null)
            {
                return null;
            }
            node.Expand();
            await node.WaitForExpansion();
            return node;
        }
    }
}
