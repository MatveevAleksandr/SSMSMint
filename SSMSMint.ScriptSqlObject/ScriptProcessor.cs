using SSMSMint.Shared.SqlObjAtPosition;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using System;
using System.Text;
using SqlObject = SSMSMint.Shared.SqlObjAtPosition.SqlObject;

namespace SSMSMint.ScriptSqlObject
{
    internal static class ScriptProcessor
    {
        public static bool TryProcess(SqlObject sqlObj, string connString, out string scriptText)
        {
            var connectionStringBuilder = new SqlConnectionStringBuilder(connString) { DataSource = sqlObj.ContextServerName };
            var serverConnection = new ServerConnection() { ConnectionString = connectionStringBuilder.ConnectionString };
            var server = new Server(serverConnection);

            var sriptingOptions = new ScriptingOptions()
            {
                ScriptData = false,
                EnforceScriptingOptions = true,
                IncludeHeaders = true,
                ScriptBatchTerminator = true,
                AnsiFile = true,
                IncludeDatabaseContext = true,
                NoCollation = true,
                WithDependencies = false
            };

            scriptText = sqlObj switch
            {
                DatabaseSqlObject => GetDatabaseScript(server, sriptingOptions, (DatabaseSqlObject)sqlObj),
                SchemaSqlObject => GetSchemaScript(server, sriptingOptions, (SchemaSqlObject)sqlObj),
                StoredProcedureSqlObject => GetStoredProcedureScript(server, sriptingOptions, (StoredProcedureSqlObject)sqlObj),
                NamedTableReferenceSqlObject => GetNamedTableReferenceScript(server, sriptingOptions, (NamedTableReferenceSqlObject)sqlObj),
                TableFunctionSqlObject => GetTableFunctionScript(server, sriptingOptions, (TableFunctionSqlObject)sqlObj),
                UserDefinedDataTypeSqlObject => GetUserDataTypeScript(server, sriptingOptions, (UserDefinedDataTypeSqlObject)sqlObj),
                ScalarFunctionSqlObject => GetScalarFunctionScript(server, sriptingOptions, (ScalarFunctionSqlObject)sqlObj),
                _ => throw new NotImplementedException($"Type {sqlObj.GetType()} is not supported for scripting")
            };

            return !string.IsNullOrWhiteSpace(scriptText);
        }

        private static string GetNamedTableReferenceScript(Server server, ScriptingOptions options, NamedTableReferenceSqlObject sqlObject)
        {
            var objUrn = server.Databases[sqlObject.ContextDatabaseName]?.Tables[sqlObject.ObjName, sqlObject.SchemaName]?.Urn ??
                         server.Databases[sqlObject.ContextDatabaseName]?.Views[sqlObject.ObjName, sqlObject.SchemaName]?.Urn ??
                         server.Databases[sqlObject.ContextDatabaseName]?.Synonyms[sqlObject.ObjName, sqlObject.SchemaName]?.Urn;

            if (objUrn == null)
            {
                return null;
            }
            options.DriAllKeys = true;
            options.Indexes = true;
            options.Triggers = true;
            options.Default = true;
            options.DriAll = true;
            options.ScriptDataCompression = true;
            options.ScriptForCreateOrAlter = true;
            options.ScriptSchema = true;
            options.Permissions = true;
            return Script(server, options, objUrn);
        }

        private static string GetUserDataTypeScript(Server server, ScriptingOptions options, UserDefinedDataTypeSqlObject sqlObject)
        {
            var userDataTypeUrn = server.Databases[sqlObject.ContextDatabaseName]?.UserDefinedDataTypes[sqlObject.ObjName, sqlObject.SchemaName]?.Urn ??
                                  server.Databases[sqlObject.ContextDatabaseName]?.UserDefinedTableTypes[sqlObject.ObjName, sqlObject.SchemaName]?.Urn;

            if (userDataTypeUrn == null)
            {
                return null;
            }
            options.ScriptForCreateOrAlter = true;
            options.DriAllKeys = true;
            options.Indexes = true;
            options.Default = true;
            options.DriAll = true;
            options.ScriptDataCompression = true;
            options.Permissions = true;
            return Script(server, options, userDataTypeUrn);
        }

        private static string GetScalarFunctionScript(Server server, ScriptingOptions options, ScalarFunctionSqlObject sqlObject)
        {
            var scalFuncUrn = server.Databases[sqlObject.ContextDatabaseName]?.UserDefinedFunctions[sqlObject.ObjName, sqlObject.SchemaName]?.Urn ??
                              server.Databases[sqlObject.ContextDatabaseName]?.UserDefinedAggregates[sqlObject.ObjName, sqlObject.SchemaName]?.Urn;
            if (scalFuncUrn == null)
            {
                return null;
            }
            options.ScriptForCreateOrAlter = true;
            return Script(server, options, scalFuncUrn);
        }

        private static string GetTableFunctionScript(Server server, ScriptingOptions options, TableFunctionSqlObject sqlObject)
        {
            var tabFunc = server.Databases[sqlObject.ContextDatabaseName]?.UserDefinedFunctions[sqlObject.ObjName, sqlObject.SchemaName];
            if (tabFunc == null)
            {
                return null;
            }
            options.ScriptForCreateOrAlter = true;
            return Script(server, options, tabFunc.Urn);
        }

        private static string GetStoredProcedureScript(Server server, ScriptingOptions options, StoredProcedureSqlObject sqlObject)
        {
            var procedure = server.Databases[sqlObject.ContextDatabaseName]?.StoredProcedures[sqlObject.ObjName, sqlObject.SchemaName];

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

                var res = new StringBuilder();
                res.AppendLine($"USE [{sqlObject.ContextDatabaseName}]");
                res.AppendLine("GO");
                res.AppendLine("SET ANSI_NULLS ON");
                res.AppendLine("GO");
                res.AppendLine("SET QUOTED_IDENTIFIER ON");
                res.AppendLine("GO");
                res.AppendLine(numberedProcedure.TextHeader);
                res.AppendLine(numberedProcedure.TextBody);
                return res.ToString();
            }

            Urn procUrn;
            if (procedure != null)
            {
                procUrn = procedure.Urn;
            }
            else
            {
                procUrn = server.Databases[sqlObject.ContextDatabaseName]?.Synonyms[sqlObject.ObjName, sqlObject.SchemaName]?.Urn;
            }

            if (procUrn == null)
            {
                return null;
            }
            options.ScriptForCreateOrAlter = true;
            return Script(server, options, procUrn);
        }

        private static string GetSchemaScript(Server server, ScriptingOptions options, SchemaSqlObject sqlObject)
        {
            var schema = server.Databases[sqlObject.ContextDatabaseName].Schemas[sqlObject.ObjName];
            if (schema == null)
            {
                return null;
            }
            return Script(server, options, schema.Urn);
        }

        private static string GetDatabaseScript(Server server, ScriptingOptions options, DatabaseSqlObject sqlObject)
        {
            var database = server.Databases[sqlObject.ObjName];
            if (database == null)
            {
                return null;
            }
            return Script(server, options, database.Urn);
        }

        private static string Script(Server server, ScriptingOptions options, Urn urn)
        {
            var scripter = new Scripter(server) { Options = options };
            var res = new StringBuilder();
            foreach (var line in scripter.Script(new[] { urn }))
            {
                res.AppendLine(line);
                res.AppendLine("GO");
            }
            return res.ToString();
        }

        //private static string GetServerScript(Server server, ScriptingOptions options)
        //{
        //    var scripter = new Scripter(server) { Options = options };

        //    // Логины
        //    var loginsScript = new StringBuilder();
        //    foreach (Login login in server.Logins)
        //    {
        //        if (login.Name.StartsWith("##")) // Пропускать системные логины
        //            continue;

        //        foreach (var line in scripter.Script(new[] { login.Urn }))
        //        {
        //            loginsScript.AppendLine(line);
        //            loginsScript.AppendLine("GO");
        //        }
        //    }

        //    // Роли
        //    var rolesScript = new StringBuilder();
        //    foreach (ServerRole role in server.Roles)
        //    {
        //        foreach (var line in scripter.Script(new[] { role.Urn }))
        //        {
        //            rolesScript.AppendLine(line);
        //            rolesScript.AppendLine("GO");
        //        }
        //    }

        //    // Конфигурации 
        //    var serverSettingsScript = new StringBuilder();
        //    foreach (ConfigProperty setting in server.Configuration.Properties)
        //    {
        //        if (setting.IsDynamic && setting.RunValue != setting.ConfigValue)
        //        {
        //            serverSettingsScript.AppendLine($"EXEC sp_configure '{setting.DisplayName}', {setting.RunValue};");
        //            serverSettingsScript.AppendLine("RECONFIGURE;");
        //            serverSettingsScript.AppendLine("GO");
        //        }
        //    }

        //    var fullScript = new StringBuilder();

        //    if (serverSettingsScript.Length > 0)
        //    {
        //        fullScript.AppendLine("-- Server Configuration Settings");
        //        fullScript.AppendLine(serverSettingsScript.ToString());
        //    }

        //    if (loginsScript.Length > 0)
        //    {
        //        fullScript.AppendLine("-- Server Logins");
        //        fullScript.AppendLine(loginsScript.ToString());
        //    }

        //    if (rolesScript.Length > 0)
        //    {
        //        fullScript.AppendLine("-- Server Roles");
        //        fullScript.AppendLine(rolesScript.ToString());
        //    }

        //    return fullScript.ToString();
        //}
    }
}