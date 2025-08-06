using Microsoft.SqlServer.TransactSql.ScriptDom;
using System.Collections.Generic;
using System.IO;

namespace SSMSMint.Shared.SqlObjAtPosition
{
    public static class SqlObjectAtPositionResolver
    {
        public static SqlObject Resolve(string sqlScriptText, int line, int column, string defaultServer, string defaultDatabase, out IList<ParseError> parseErrors)
        {
            var parser = new TSql150Parser(true);
            var parsedSqlText = parser.Parse(new StringReader(sqlScriptText), out parseErrors);

            var visitor = new SqlObjectAtPositionVisitor(line, column, defaultServer, defaultDatabase);
            parsedSqlText.Accept(visitor);

            return visitor.SqlObjectUnderCursor;
        }
    }
}
