using SSMSMint.Core.Models;
using System.Threading.Tasks;

namespace SSMSMint.Core.Interfaces;

public interface IObjectExplorerManager
{
    public Task<bool> TryFindObjNodeAsync(SqlObject sqlObject);
    public void ConnectToServer(SqlConnectionMetaData sqlConnection);
}
