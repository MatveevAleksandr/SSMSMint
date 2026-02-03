using SSMSMint.Core;
using SSMSMint.Core.Interfaces;
using SSMSMint.Core.Models;
using SSMSMint.Core.Resources;
using Microsoft.SqlServer.Management.Smo.RegSvrEnum;
using Microsoft.SqlServer.Management.UI.VSIntegration.ObjectExplorer;
using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace SSMSMint.SSMS2022.Implementations;

internal class ObjectExplorerManagerImpl(IObjectExplorerService oeService) : IObjectExplorerManager
{
    public void ConnectToServer(SqlConnectionMetaData sqlConnection)
    {
        var uiConn = new UIConnectionInfo()
        {
            ServerName = sqlConnection.ServerName,
            AuthenticationType = (int)sqlConnection.AuthType,
            ApplicationName = sqlConnection.AppName,
            ServerType = Consts.DBEServerType
        };
        uiConn.AdvancedOptions.Add("TRUST_SERVER_CERTIFICATE", "true");
        oeService.ConnectToServer(uiConn);
    }

    public async Task<bool> TryFindObjNodeAsync(SqlObject sqlObject)
    {
        var treeView = (TreeView)oeService.GetType().GetProperty("Tree", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(oeService);

        var nodes = treeView.Nodes;

        var node = sqlObject switch
        {
            ServerSqlObject => FindServerNode(sqlObject.ObjName, nodes),
            DatabaseSqlObject => await FindDatabaseNode(sqlObject.ContextServerName, sqlObject.ObjName, nodes),
            StoredProcedureSqlObject => await FindProcedureNode((StoredProcedureSqlObject)sqlObject, nodes),
            NamedTableReferenceSqlObject => await FindNamedTableReferenceNode((NamedTableReferenceSqlObject)sqlObject, nodes),
            TableFunctionSqlObject => await FindTableFunctionNode((TableFunctionSqlObject)sqlObject, nodes),
            SchemaSqlObject => await FindSchemaNode((SchemaSqlObject)sqlObject, nodes),
            UserDefinedDataTypeSqlObject => await FindUserDefinedDataTypeNode((UserDefinedDataTypeSqlObject)sqlObject, nodes),
            ScalarFunctionSqlObject => await FindScalarFunctionNode((ScalarFunctionSqlObject)sqlObject, nodes),
            _ => throw new NotImplementedException($"Type {sqlObject.GetType()} is not supported for locating")
        };

        if (node == null)
        {
            return false;
        }

        // Focus node
        treeView.SelectedNode = node;

        return true;
    }

    private HierarchyTreeNode FindServerNode(string serverName, TreeNodeCollection nodes) => FindNode(nodes, serverName);

    private async Task<HierarchyTreeNode> FindDatabaseNode(string serverName, string sqlObjName, TreeNodeCollection nodes)
    {
        var serverNode = FindServerNode(serverName, nodes);
        if (serverNode == null) { return null; }
        serverNode.Expand();
        await WaitForExpansionAsync(serverNode);

        var databasesNode = await FindAndExpandNodeAsync(serverNode, Localization.DatabasesNodeName);
        if (databasesNode == null) { return null; }

        if (sqlObjName.ToLower() == "master" || sqlObjName.ToLower() == "model" || sqlObjName.ToLower() == "msdb" || sqlObjName.ToLower() == "tempdb")
        {
            var systemDatabasesNode = await FindAndExpandNodeAsync(databasesNode, Localization.SystemDatabasesNodeName);
            if (systemDatabasesNode == null) { return null; }

            return FindNode(systemDatabasesNode.Nodes, sqlObjName);
        }

        return FindNode(databasesNode.Nodes, sqlObjName);
    }

    private async Task<HierarchyTreeNode> FindAndExpandDatabaseNode(string serverName, string sqlObjName, TreeNodeCollection nodes)
    {
        var databaseNode = await FindDatabaseNode(serverName, sqlObjName, nodes);
        if (databaseNode == null) { return null; }
        databaseNode.Expand();
        await WaitForExpansionAsync(databaseNode);
        return databaseNode;
    }

    private async Task<HierarchyTreeNode> FindSchemaNode(SchemaSqlObject sqlObj, TreeNodeCollection nodes)
    {
        var databaseNode = await FindAndExpandDatabaseNode(sqlObj.ContextServerName, sqlObj.ContextDatabaseName, nodes);
        if (databaseNode == null) { return null; }
        var securityNode = await FindAndExpandNodeAsync(databaseNode, Localization.SecurityNodeName);
        if (securityNode == null) { return null; }
        var schemasProcNode = await FindAndExpandNodeAsync(securityNode, Localization.SchemasNodeName);
        if (schemasProcNode == null) { return null; }

        return FindNode(schemasProcNode.Nodes, sqlObj.ObjName);
    }

    private async Task<HierarchyTreeNode> FindProcedureNode(StoredProcedureSqlObject sqlObj, TreeNodeCollection nodes)
    {
        var databaseNode = await FindAndExpandDatabaseNode(sqlObj.ContextServerName, sqlObj.ContextDatabaseName, nodes);
        if (databaseNode == null) { return null; }
        var programmabilityNode = await FindAndExpandNodeAsync(databaseNode, Localization.ProgrammabilityNodeName);
        if (programmabilityNode == null) { return null; }

        var storedProcNode = await FindAndExpandNodeAsync(programmabilityNode, Localization.StoredProcNodeName);
        if (storedProcNode != null)
        {
            var storedProc = FindNode(storedProcNode.Nodes, $"{sqlObj.SchemaName}.{sqlObj.ObjName}");
            if (storedProc != null)
            {
                return storedProc;
            }
            storedProcNode.Collapse();
            programmabilityNode.Collapse();
        }

        var synonym = await FindSynonymNode(databaseNode, $"{sqlObj.SchemaName}.{sqlObj.ObjName}");
        if (synonym != null)
        {
            return synonym;
        }

        return null;
    }

    private async Task<HierarchyTreeNode> FindTableFunctionNode(TableFunctionSqlObject sqlObj, TreeNodeCollection nodes)
    {
        var databaseNode = await FindAndExpandDatabaseNode(sqlObj.ContextServerName, sqlObj.ContextDatabaseName, nodes);
        if (databaseNode == null) { return null; }
        var programmabilityNode = await FindAndExpandNodeAsync(databaseNode, Localization.ProgrammabilityNodeName);
        if (programmabilityNode == null) { return null; }
        var functionsNode = await FindAndExpandNodeAsync(programmabilityNode, Localization.FunctionsNodeName);
        if (functionsNode == null) { return null; }
        var tableValuedFunctionsNode = await FindAndExpandNodeAsync(functionsNode, Localization.TableValuedFunctionsNodeName);
        if (tableValuedFunctionsNode == null) { return null; }

        return FindNode(tableValuedFunctionsNode.Nodes, $"{sqlObj.SchemaName}.{sqlObj.ObjName}");
    }

    private async Task<HierarchyTreeNode> FindNamedTableReferenceNode(NamedTableReferenceSqlObject sqlObj, TreeNodeCollection nodes)
    {
        var sqlObjTitle = $"{sqlObj.SchemaName}.{sqlObj.ObjName}";
        var databaseNode = await FindAndExpandDatabaseNode(sqlObj.ContextServerName, sqlObj.ContextDatabaseName, nodes);
        if (databaseNode == null) { return null; }

        var viewsNode = await FindAndExpandNodeAsync(databaseNode, Localization.ViewsNodeName);
        if (viewsNode != null)
        {
            var view = FindNode(viewsNode.Nodes, sqlObjTitle);
            if (view != null)
            {
                return view;
            }
            viewsNode.Collapse();
        }

        var tablesNode = await FindAndExpandNodeAsync(databaseNode, Localization.TablesNodeName);
        if (tablesNode != null)
        {
            var table = FindNode(tablesNode.Nodes, sqlObjTitle);
            if (table != null)
            {
                return table;
            }
            tablesNode.Collapse();
        }

        var synonym = await FindSynonymNode(databaseNode, sqlObjTitle);
        if (synonym != null)
        {
            return synonym;
        }

        return null;
    }

    private async Task<HierarchyTreeNode> FindUserDefinedDataTypeNode(UserDefinedDataTypeSqlObject sqlObj, TreeNodeCollection nodes)
    {
        var databaseNode = await FindAndExpandDatabaseNode(sqlObj.ContextServerName, sqlObj.ContextDatabaseName, nodes);
        if (databaseNode == null) { return null; }
        var programmabilityNode = await FindAndExpandNodeAsync(databaseNode, Localization.ProgrammabilityNodeName);
        if (programmabilityNode == null) { return null; }
        var typesNode = await FindAndExpandNodeAsync(programmabilityNode, Localization.TypesNodeName);
        if (typesNode == null) { return null; }

        var userDefinedDataTypesNode = await FindAndExpandNodeAsync(typesNode, Localization.UserDefinedDataTypesNodeName);
        if (userDefinedDataTypesNode != null)
        {
            var dataType = FindNode(userDefinedDataTypesNode.Nodes, $"{sqlObj.SchemaName}.{sqlObj.ObjName}");
            if (dataType != null)
            {
                return dataType;
            }
            userDefinedDataTypesNode.Collapse();
        }

        var userDefinedTableTypesNode = await FindAndExpandNodeAsync(typesNode, Localization.UserDefinedTableTypesNodeName);
        if (userDefinedTableTypesNode != null)
        {
            var tableType = FindNode(userDefinedTableTypesNode.Nodes, $"{sqlObj.SchemaName}.{sqlObj.ObjName}");
            if (tableType != null)
            {
                return tableType;
            }
            userDefinedTableTypesNode.Collapse();
        }

        return null;
    }

    private async Task<HierarchyTreeNode> FindScalarFunctionNode(ScalarFunctionSqlObject sqlObj, TreeNodeCollection nodes)
    {
        var databaseNode = await FindAndExpandDatabaseNode(sqlObj.ContextServerName, sqlObj.ContextDatabaseName, nodes);
        if (databaseNode == null) { return null; }
        var programmabilityNode = await FindAndExpandNodeAsync(databaseNode, Localization.ProgrammabilityNodeName);
        if (programmabilityNode == null) { return null; }
        var functionsNode = await FindAndExpandNodeAsync(programmabilityNode, Localization.FunctionsNodeName);
        if (functionsNode == null) { return null; }

        var scalarValuedFunctionsNode = await FindAndExpandNodeAsync(functionsNode, Localization.ScalarValuedFunctionsNodeName);
        if (scalarValuedFunctionsNode != null)
        {
            var scalarValuedFunction = FindNode(scalarValuedFunctionsNode.Nodes, $"{sqlObj.SchemaName}.{sqlObj.ObjName}");
            if (scalarValuedFunction != null)
            {
                return scalarValuedFunction;
            }
            scalarValuedFunctionsNode.Collapse();
        }

        var aggregateFunctionsNode = await FindAndExpandNodeAsync(functionsNode, Localization.AggregateFunctionsNodeName);
        if (aggregateFunctionsNode != null)
        {
            var aggregateFunction = FindNode(aggregateFunctionsNode.Nodes, $"{sqlObj.SchemaName}.{sqlObj.ObjName}");
            if (aggregateFunction != null)
            {
                return aggregateFunction;
            }
            aggregateFunctionsNode.Collapse();
        }

        return null;
    }

    private async Task<HierarchyTreeNode> FindSynonymNode(HierarchyTreeNode databaseNode, string synonimTitle)
    {
        var synonymsNode = await FindAndExpandNodeAsync(databaseNode, Localization.SynonymsNodeName);
        if (synonymsNode != null)
        {
            var synonym = FindNode(synonymsNode.Nodes, synonimTitle);
            if (synonym != null)
            {
                return synonym;
            }
            synonymsNode.Collapse();
        }

        return null;
    }

    private async Task WaitForExpansionAsync(HierarchyTreeNode node)
    {
        int attempts = 0;

        while (attempts < 100 && node.Expanding)
        {
            await Task.Delay(300);
            attempts++;
        }
    }

    private HierarchyTreeNode FindNode(TreeNodeCollection nodes, string searchNodeText)
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

    private async Task<HierarchyTreeNode> FindAndExpandNodeAsync(HierarchyTreeNode rootNode, string searchNodeText)
    {
        if (rootNode == null)
        {
            return null;
        }

        var node = FindNode(rootNode.Nodes, searchNodeText);
        if (node == null)
        {
            return null;
        }

        // Если нода фильтрована, то фильтр надо сбросить чтобы было из чего искать
        var containedItem = node.GetType().GetProperty("ContainedItem", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).GetValue(node);
        containedItem.GetType().GetProperty("Filter", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).SetValue(containedItem, null);

        node.Expand();
        await WaitForExpansionAsync(node);
        return node;
    }
}
