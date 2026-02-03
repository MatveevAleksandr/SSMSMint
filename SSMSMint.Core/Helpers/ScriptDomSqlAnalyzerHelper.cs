using SSMSMint.Core.Models;
using SSMSMint.Core.Visitors;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using System.Collections.Generic;
using System.IO;

namespace SSMSMint.Core.Helpers;

public static class ScriptDomSqlAnalyzerHelper
{
    public static SqlObject GetSqlObjectAtPosition(string sqlText, TextPoint point, string defaultServer, string defaultDatabase, out IList<ParseError> parseErrors)
    {
        var parser = new TSql150Parser(true);
        var parsedSqlText = parser.Parse(new StringReader(sqlText), out parseErrors);

        var visitor = new SqlObjectAtPositionVisitor(point, defaultServer, defaultDatabase);
        parsedSqlText.Accept(visitor);

        return visitor.SqlObjectUnderCursor;
    }
}
