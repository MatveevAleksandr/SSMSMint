using SSMSMint.Core.Models;

namespace SSMSMint.Core.Interfaces;

public interface ISqlScriptProcessorManager
{
    public string GetSqlObjectScript(SqlObject sqlObject, SqlConnectionMetaData sqlConnection);
}
