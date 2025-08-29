using SSMSMint.Shared.SqlObjAtPosition;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.UI.VSIntegration.Editors;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace SSMSMint.Shared.Extentions;

public static class SqlScriptEditorControlExtentions
{
    public static SqlConnection GetSqlConnection(this SqlScriptEditorControl control) => (SqlConnection)control.GetType().GetField("m_connection", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(control);

    public static void GetSqlObjectAtPosition(this SqlScriptEditorControl control, int line, int column, out IList<ParseError> parseErrors, out SqlObject sqlObject)
    {
        var editorConnection = control.GetSqlConnection() ?? throw new Exception("Editor SQLConnection not found");
        sqlObject = SqlObjectAtPositionResolver.Resolve(control.EditorText, line, column, control.Connection.ServerName, editorConnection.Database, out parseErrors);
    }
}
