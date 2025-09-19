using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.UserSettings;
using SSMSMint.Shared.Settings;
using SSMSMint.Shared.SqlObjAtPosition;
using System;
using System.Text;
using ScriptingOptions = Microsoft.SqlServer.Management.Smo.ScriptingOptions;
using SqlObject = SSMSMint.Shared.SqlObjAtPosition.SqlObject;

namespace SSMSMint.ScriptSqlObject;

internal static class ScriptProcessor
{
    public static bool TryProcess(SqlObject sqlObj, string connString, SSMSMintSettings settings, out string scriptText)
    {
        var connectionStringBuilder = new SqlConnectionStringBuilder(connString) { DataSource = sqlObj.ContextServerName };
        var serverConnection = new ServerConnection() { ConnectionString = connectionStringBuilder.ConnectionString };
        var server = new Server(serverConnection);

        scriptText = sqlObj switch
        {
            DatabaseSqlObject => GetDatabaseScript(server, (DatabaseSqlObject)sqlObj),
            SchemaSqlObject => GetSchemaScript(server, (SchemaSqlObject)sqlObj),
            StoredProcedureSqlObject => GetStoredProcedureScript(server, (StoredProcedureSqlObject)sqlObj, settings),
            NamedTableReferenceSqlObject => GetNamedTableReferenceScript(server, (NamedTableReferenceSqlObject)sqlObj),
            TableFunctionSqlObject => GetTableFunctionScript(server, (TableFunctionSqlObject)sqlObj),
            UserDefinedDataTypeSqlObject => GetUserDataTypeScript(server, (UserDefinedDataTypeSqlObject)sqlObj),
            ScalarFunctionSqlObject => GetScalarFunctionScript(server, (ScalarFunctionSqlObject)sqlObj),
            _ => throw new NotImplementedException($"Type {sqlObj.GetType()} is not supported for scripting")
        };

        return !string.IsNullOrWhiteSpace(scriptText);
    }

    private static string GetNamedTableReferenceScript(Server server, NamedTableReferenceSqlObject sqlObject)
    {
        var objUrn = server.Databases[sqlObject.ContextDatabaseName]?.Tables[sqlObject.ObjName, sqlObject.SchemaName]?.Urn ??
                     server.Databases[sqlObject.ContextDatabaseName]?.Views[sqlObject.ObjName, sqlObject.SchemaName]?.Urn ??
                     server.Databases[sqlObject.ContextDatabaseName]?.Synonyms[sqlObject.ObjName, sqlObject.SchemaName]?.Urn;
        return GetUrnScript(server, objUrn);
    }

    private static string GetUserDataTypeScript(Server server, UserDefinedDataTypeSqlObject sqlObject)
    {
        var userDataTypeUrn = server.Databases[sqlObject.ContextDatabaseName]?.UserDefinedDataTypes[sqlObject.ObjName, sqlObject.SchemaName]?.Urn ??
                              server.Databases[sqlObject.ContextDatabaseName]?.UserDefinedTableTypes[sqlObject.ObjName, sqlObject.SchemaName]?.Urn;
        return GetUrnScript(server, userDataTypeUrn);
    }

    private static string GetScalarFunctionScript(Server server, ScalarFunctionSqlObject sqlObject)
    {
        var scalFuncUrn = server.Databases[sqlObject.ContextDatabaseName]?.UserDefinedFunctions[sqlObject.ObjName, sqlObject.SchemaName]?.Urn ??
                          server.Databases[sqlObject.ContextDatabaseName]?.UserDefinedAggregates[sqlObject.ObjName, sqlObject.SchemaName]?.Urn;
        return GetUrnScript(server, scalFuncUrn);
    }

    private static string GetTableFunctionScript(Server server, TableFunctionSqlObject sqlObject)
    {
        var tabFuncUrn = server.Databases[sqlObject.ContextDatabaseName]?.UserDefinedFunctions[sqlObject.ObjName, sqlObject.SchemaName]?.Urn;
        return GetUrnScript(server, tabFuncUrn);
    }

    private static string GetStoredProcedureScript(Server server, StoredProcedureSqlObject sqlObject, SSMSMintSettings settings)
    {
        var procedure = server.Databases[sqlObject.ContextDatabaseName]?.StoredProcedures[sqlObject.ObjName, sqlObject.SchemaName];
        // Номерная процедура
        // Scripter не умеет работать с Number в Urn. т.к. это в целом старье, то оставим просто так
        if (
                procedure != null &&
                !string.IsNullOrWhiteSpace(sqlObject.Number) &&
                Convert.ToInt16(sqlObject.Number) != 1 // Номерная проца с номером 1 тут не прочитается. Она равна обычной, поэтому ее пустим дальше
            )
        {
            var numberedProcedure = procedure.NumberedStoredProcedures?.GetProcedureByNumber(Convert.ToInt16(sqlObject.Number));

            if (numberedProcedure == null)
            {
                return null;
            }

            return GetNumberedProcedureScript(numberedProcedure, sqlObject.ContextDatabaseName);
        }
        // Обычная процедура без номера
        else if (procedure != null)
        {
            var res = new StringBuilder();
            res.AppendLine(GetUrnScript(server, procedure.Urn));

            // Если скриптуем обычную процу, то при включенной настройке скриптуем все её номерные версии при наличии
            if (settings.IncludeNumberedProcedures)
            {
                foreach (NumberedStoredProcedure numberedProcedure in procedure.NumberedStoredProcedures)
                {
                    res.AppendLine("-- =========================================================================================================================== --");
                    var script = GetNumberedProcedureScript(numberedProcedure, sqlObject.ContextDatabaseName);
                    res.AppendLine(script);
                }
            }

            return res.ToString();
        }
        // Синоним
        else
        {
            var synonymUrn = server.Databases[sqlObject.ContextDatabaseName]?.Synonyms[sqlObject.ObjName, sqlObject.SchemaName]?.Urn;
            return GetUrnScript(server, synonymUrn);
        }
    }

    private static string GetSchemaScript(Server server, SchemaSqlObject sqlObject)
    {
        var schemaUrn = server.Databases[sqlObject.ContextDatabaseName].Schemas[sqlObject.ObjName]?.Urn;
        return GetUrnScript(server, schemaUrn);
    }

    private static string GetDatabaseScript(Server server, DatabaseSqlObject sqlObject)
    {
        var databaseUrn = server.Databases[sqlObject.ObjName]?.Urn;
        return GetUrnScript(server, databaseUrn);
    }

    private static string GetNumberedProcedureScript(NumberedStoredProcedure procedure, string contextDataBaseName)
    {
        var res = new StringBuilder();
        res.AppendLine($"USE [{contextDataBaseName}]");
        res.AppendLine("GO");
        res.AppendLine("SET ANSI_NULLS ON");
        res.AppendLine("GO");
        res.AppendLine("SET QUOTED_IDENTIFIER ON");
        res.AppendLine("GO");
        res.AppendLine(procedure.TextHeader);
        res.AppendLine(procedure.TextBody);
        res.AppendLine("GO");
        return res.ToString();
    }

    private static string GetUrnScript(Server server, Urn urn)
    {
        if (urn == null)
        {
            return null;
        }

        // Возьмем настройки скриптования из Options - SQL Server Object Explorer - Scripting
        var sriptingOptions = (ScriptingOptions)Settings<SqlStudio>.Current.SSMS.ScriptingOptions.GetSmoScriptingOptions();
        var scripter = new Scripter(server) { Options = sriptingOptions };
        var res = new StringBuilder();
        foreach (var line in scripter.Script(new[] { urn }))
        {
            res.AppendLine(line);
            res.AppendLine("GO");
        }
        return res.ToString();
    }
}