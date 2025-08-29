using SSMSMint.Shared.Extentions;
using SSMSMint.Shared.Res;
using SSMSMint.Shared.SqlObjAtPosition;
using Microsoft.SqlServer.Management.UI.VSIntegration.ObjectExplorer;
using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SSMSMint.LocateInObjectExplorer;

internal static class LocateProcessor
{
    public static async Task<bool> TryLocateAsync(TreeView treeView, SqlObject sqlObj)
    {
        var nodes = treeView.Nodes;

        var node = sqlObj switch
        {
            ServerSqlObject => FindServerNode(sqlObj.ObjName, nodes),
            DatabaseSqlObject => await FindDatabaseNode(sqlObj.ContextServerName, sqlObj.ObjName, nodes),
            StoredProcedureSqlObject => await FindProcedureNode((StoredProcedureSqlObject)sqlObj, nodes),
            NamedTableReferenceSqlObject => await FindNamedTableReferenceNode((NamedTableReferenceSqlObject)sqlObj, nodes),
            TableFunctionSqlObject => await FindTableFunctionNode((TableFunctionSqlObject)sqlObj, nodes),
            SchemaSqlObject => await FindSchemaNode((SchemaSqlObject)sqlObj, nodes),
            UserDefinedDataTypeSqlObject => await FindUserDefinedDataTypeNode((UserDefinedDataTypeSqlObject)sqlObj, nodes),
            ScalarFunctionSqlObject => await FindScalarFunctionNode((ScalarFunctionSqlObject)sqlObj, nodes),
            _ => throw new NotImplementedException($"Type {sqlObj.GetType()} is not supported for locating")
        };

        if (node == null)
        {
            return false;
        }

        // Focus node
        treeView.SelectedNode = node;

        return true;
    }

    private static HierarchyTreeNode FindServerNode(string serverName, TreeNodeCollection nodes) => nodes.FindNode(serverName);

    private static async Task<HierarchyTreeNode> FindDatabaseNode(string serverName, string sqlObjName, TreeNodeCollection nodes)
    {
        var serverNode = FindServerNode(serverName, nodes);
        if (serverNode == null) { return null; }
        serverNode.Expand();
        await serverNode.WaitForExpansion();

        var databasesNode = await serverNode.FindAndExpandNode(Localization.DatabasesNodeName);
        if (databasesNode == null) { return null; }

        if (sqlObjName.ToLower() == "master" || sqlObjName.ToLower() == "model" || sqlObjName.ToLower() == "msdb" || sqlObjName.ToLower() == "tempdb")
        {
            var systemDatabasesNode = await databasesNode.FindAndExpandNode(Localization.SystemDatabasesNodeName);
            if (systemDatabasesNode == null) { return null; }

            return systemDatabasesNode.Nodes.FindNode(sqlObjName);
        }

        return databasesNode.Nodes.FindNode(sqlObjName);
    }

    private static async Task<HierarchyTreeNode> FindAndExpandDatabaseNode(string serverName, string sqlObjName, TreeNodeCollection nodes)
    {
        var databaseNode = await FindDatabaseNode(serverName, sqlObjName, nodes);
        if (databaseNode == null) { return null; }
        databaseNode.Expand();
        await databaseNode.WaitForExpansion();
        return databaseNode;
    }

    private static async Task<HierarchyTreeNode> FindSchemaNode(SchemaSqlObject sqlObj, TreeNodeCollection nodes)
    {
        var databaseNode = await FindAndExpandDatabaseNode(sqlObj.ContextServerName, sqlObj.ContextDatabaseName, nodes);
        if (databaseNode == null) { return null; }
        var securityNode = await databaseNode.FindAndExpandNode(Localization.SecurityNodeName);
        if (securityNode == null) { return null; }
        var schemasProcNode = await securityNode.FindAndExpandNode(Localization.SchemasNodeName);
        if (schemasProcNode == null) { return null; }

        return schemasProcNode.Nodes.FindNode(sqlObj.ObjName);
    }

    private static async Task<HierarchyTreeNode> FindProcedureNode(StoredProcedureSqlObject sqlObj, TreeNodeCollection nodes)
    {
        var databaseNode = await FindAndExpandDatabaseNode(sqlObj.ContextServerName, sqlObj.ContextDatabaseName, nodes);
        if (databaseNode == null) { return null; }
        var programmabilityNode = await databaseNode.FindAndExpandNode(Localization.ProgrammabilityNodeName);
        if (programmabilityNode == null) { return null; }

        var storedProcNode = await programmabilityNode.FindAndExpandNode(Localization.StoredProcNodeName);
        if (storedProcNode != null)
        {
            var storedProc = storedProcNode.Nodes.FindNode($"{sqlObj.SchemaName}.{sqlObj.ObjName}");
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

    private static async Task<HierarchyTreeNode> FindTableFunctionNode(TableFunctionSqlObject sqlObj, TreeNodeCollection nodes)
    {
        var databaseNode = await FindAndExpandDatabaseNode(sqlObj.ContextServerName, sqlObj.ContextDatabaseName, nodes);
        if (databaseNode == null) { return null; }
        var programmabilityNode = await databaseNode.FindAndExpandNode(Localization.ProgrammabilityNodeName);
        if (programmabilityNode == null) { return null; }
        var functionsNode = await programmabilityNode.FindAndExpandNode(Localization.FunctionsNodeName);
        if (functionsNode == null) { return null; }
        var tableValuedFunctionsNode = await functionsNode.FindAndExpandNode(Localization.TableValuedFunctionsNodeName);
        if (tableValuedFunctionsNode == null) { return null; }

        return tableValuedFunctionsNode.Nodes.FindNode($"{sqlObj.SchemaName}.{sqlObj.ObjName}");
    }

    private static async Task<HierarchyTreeNode> FindNamedTableReferenceNode(NamedTableReferenceSqlObject sqlObj, TreeNodeCollection nodes)
    {
        var sqlObjTitle = $"{sqlObj.SchemaName}.{sqlObj.ObjName}";
        var databaseNode = await FindAndExpandDatabaseNode(sqlObj.ContextServerName, sqlObj.ContextDatabaseName, nodes);
        if (databaseNode == null) { return null; }

        var viewsNode = await databaseNode.FindAndExpandNode(Localization.ViewsNodeName);
        if (viewsNode != null)
        {
            var view = viewsNode.Nodes.FindNode(sqlObjTitle);
            if (view != null)
            {
                return view;
            }
            viewsNode.Collapse();
        }

        var tablesNode = await databaseNode.FindAndExpandNode(Localization.TablesNodeName);
        if (tablesNode != null)
        {
            var table = tablesNode.Nodes.FindNode(sqlObjTitle);
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

    private static async Task<HierarchyTreeNode> FindUserDefinedDataTypeNode(UserDefinedDataTypeSqlObject sqlObj, TreeNodeCollection nodes)
    {
        var databaseNode = await FindAndExpandDatabaseNode(sqlObj.ContextServerName, sqlObj.ContextDatabaseName, nodes);
        if (databaseNode == null) { return null; }
        var programmabilityNode = await databaseNode.FindAndExpandNode(Localization.ProgrammabilityNodeName);
        if (programmabilityNode == null) { return null; }
        var typesNode = await programmabilityNode.FindAndExpandNode(Localization.TypesNodeName);
        if (typesNode == null) { return null; }

        var userDefinedDataTypesNode = await typesNode.FindAndExpandNode(Localization.UserDefinedDataTypesNodeName);
        if (userDefinedDataTypesNode != null)
        {
            var dataType = userDefinedDataTypesNode.Nodes.FindNode($"{sqlObj.SchemaName}.{sqlObj.ObjName}");
            if (dataType != null)
            {
                return dataType;
            }
            userDefinedDataTypesNode.Collapse();
        }

        var userDefinedTableTypesNode = await typesNode.FindAndExpandNode(Localization.UserDefinedTableTypesNodeName);
        if (userDefinedTableTypesNode != null)
        {
            var tableType = userDefinedTableTypesNode.Nodes.FindNode($"{sqlObj.SchemaName}.{sqlObj.ObjName}");
            if (tableType != null)
            {
                return tableType;
            }
            userDefinedTableTypesNode.Collapse();
        }

        return null;
    }

    private static async Task<HierarchyTreeNode> FindScalarFunctionNode(ScalarFunctionSqlObject sqlObj, TreeNodeCollection nodes)
    {
        var databaseNode = await FindAndExpandDatabaseNode(sqlObj.ContextServerName, sqlObj.ContextDatabaseName, nodes);
        if (databaseNode == null) { return null; }
        var programmabilityNode = await databaseNode.FindAndExpandNode(Localization.ProgrammabilityNodeName);
        if (programmabilityNode == null) { return null; }
        var functionsNode = await programmabilityNode.FindAndExpandNode(Localization.FunctionsNodeName);
        if (functionsNode == null) { return null; }

        var scalarValuedFunctionsNode = await functionsNode.FindAndExpandNode(Localization.ScalarValuedFunctionsNodeName);
        if (scalarValuedFunctionsNode != null)
        {
            var scalarValuedFunction = scalarValuedFunctionsNode.Nodes.FindNode($"{sqlObj.SchemaName}.{sqlObj.ObjName}");
            if (scalarValuedFunction != null)
            {
                return scalarValuedFunction;
            }
            scalarValuedFunctionsNode.Collapse();
        }

        var aggregateFunctionsNode = await functionsNode.FindAndExpandNode(Localization.AggregateFunctionsNodeName);
        if (aggregateFunctionsNode != null)
        {
            var aggregateFunction = aggregateFunctionsNode.Nodes.FindNode($"{sqlObj.SchemaName}.{sqlObj.ObjName}");
            if (aggregateFunction != null)
            {
                return aggregateFunction;
            }
            aggregateFunctionsNode.Collapse();
        }

        return null;
    }

    private static async Task<HierarchyTreeNode> FindSynonymNode(HierarchyTreeNode databaseNode, string synonimTitle)
    {
        var synonymsNode = await databaseNode.FindAndExpandNode(Localization.SynonymsNodeName);
        if (synonymsNode != null)
        {
            var synonym = synonymsNode.Nodes.FindNode(synonimTitle);
            if (synonym != null)
            {
                return synonym;
            }
            synonymsNode.Collapse();
        }

        return null;
    }
}
