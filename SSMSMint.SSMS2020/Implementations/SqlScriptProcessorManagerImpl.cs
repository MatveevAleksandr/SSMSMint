using SSMSMint.Core.Interfaces;
using SSMSMint.Core.Models;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.UserSettings;
using System;
using System.Text;

namespace SSMSMint.SSMS2020.Implementations;

internal class SqlScriptProcessorManagerImpl(ISettingsManager settingsManager) : ISqlScriptProcessorManager
{
    public string GetSqlObjectScript(Core.Models.SqlObject sqlObject, SqlConnectionMetaData sqlConnection)
    {
        var serverConnection = new ServerConnection()
        {
            ServerInstance = sqlConnection.ServerName,
            DatabaseName = sqlConnection.DatabaseName,
            ApplicationName = sqlConnection.AppName
        };
        var server = new Server(serverConnection);
        var settings = settingsManager.GetSettings();

        return sqlObject switch
        {
            DatabaseSqlObject => GetDatabaseScript(server, (DatabaseSqlObject)sqlObject),
            SchemaSqlObject => GetSchemaScript(server, (SchemaSqlObject)sqlObject),
            StoredProcedureSqlObject => GetStoredProcedureScript(server, (StoredProcedureSqlObject)sqlObject, settings),
            NamedTableReferenceSqlObject => GetNamedTableReferenceScript(server, (NamedTableReferenceSqlObject)sqlObject),
            TableFunctionSqlObject => GetTableFunctionScript(server, (TableFunctionSqlObject)sqlObject),
            UserDefinedDataTypeSqlObject => GetUserDataTypeScript(server, (UserDefinedDataTypeSqlObject)sqlObject),
            ScalarFunctionSqlObject => GetScalarFunctionScript(server, (ScalarFunctionSqlObject)sqlObject),
            _ => throw new NotImplementedException($"Type {sqlObject.GetType()} is not supported for scripting")
        };
    }

    private string GetNamedTableReferenceScript(Server server, NamedTableReferenceSqlObject sqlObject)
    {
        var objUrn = server.Databases[sqlObject.ContextDatabaseName]?.Tables[sqlObject.ObjName, sqlObject.SchemaName]?.Urn ??
                     server.Databases[sqlObject.ContextDatabaseName]?.Views[sqlObject.ObjName, sqlObject.SchemaName]?.Urn ??
                     server.Databases[sqlObject.ContextDatabaseName]?.Synonyms[sqlObject.ObjName, sqlObject.SchemaName]?.Urn;
        return GetUrnScript(server, objUrn);
    }

    private string GetUserDataTypeScript(Server server, UserDefinedDataTypeSqlObject sqlObject)
    {
        var userDataTypeUrn = server.Databases[sqlObject.ContextDatabaseName]?.UserDefinedDataTypes[sqlObject.ObjName, sqlObject.SchemaName]?.Urn ??
                              server.Databases[sqlObject.ContextDatabaseName]?.UserDefinedTableTypes[sqlObject.ObjName, sqlObject.SchemaName]?.Urn;
        return GetUrnScript(server, userDataTypeUrn);
    }

    private string GetScalarFunctionScript(Server server, ScalarFunctionSqlObject sqlObject)
    {
        var scalFuncUrn = server.Databases[sqlObject.ContextDatabaseName]?.UserDefinedFunctions[sqlObject.ObjName, sqlObject.SchemaName]?.Urn ??
                          server.Databases[sqlObject.ContextDatabaseName]?.UserDefinedAggregates[sqlObject.ObjName, sqlObject.SchemaName]?.Urn;
        return GetUrnScript(server, scalFuncUrn);
    }

    private string GetTableFunctionScript(Server server, TableFunctionSqlObject sqlObject)
    {
        var tabFuncUrn = server.Databases[sqlObject.ContextDatabaseName]?.UserDefinedFunctions[sqlObject.ObjName, sqlObject.SchemaName]?.Urn;
        return GetUrnScript(server, tabFuncUrn);
    }

    private string GetStoredProcedureScript(Server server, StoredProcedureSqlObject sqlObject, SSMSMintSettings settings)
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

    private string GetSchemaScript(Server server, SchemaSqlObject sqlObject)
    {
        var schemaUrn = server.Databases[sqlObject.ContextDatabaseName].Schemas[sqlObject.ObjName]?.Urn;
        return GetUrnScript(server, schemaUrn);
    }

    private string GetDatabaseScript(Server server, DatabaseSqlObject sqlObject)
    {
        var databaseUrn = server.Databases[sqlObject.ObjName]?.Urn;
        return GetUrnScript(server, databaseUrn);
    }

    private string GetNumberedProcedureScript(NumberedStoredProcedure procedure, string contextDataBaseName)
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

    private string GetUrnScript(Server server, Urn urn)
    {
        if (urn == null)
        {
            return null;
        }
        // Возьмем настройки скриптования из Options - SQL Server Object Explorer - Scripting
        var smoOptions = (Microsoft.SqlServer.Management.Smo.ScriptingOptions)Settings<SqlStudio>.Current.SSMS.ScriptingOptions.GetSmoScriptingOptions();
        var scripter = new Scripter(server) { Options = smoOptions };
        var res = new StringBuilder();
        foreach (var line in scripter.Script(new[] { urn }))
        {
            res.AppendLine(line);
            res.AppendLine("GO");
        }
        return res.ToString();
    }
}
