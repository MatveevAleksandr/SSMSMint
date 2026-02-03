using SSMSMint.Core.Models;
using SSMSMint.Core.UI.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SSMSMint.Core.Interfaces;

public interface IWorkspaceManager
{
    public SqlConnectionMetaData GetActiveEditorConnection();

    public ITextDocumentManager CreateNewSqlBlankScript();
    public Task<ITextDocumentManager> CreateNewFileAsync(string type, string name);

    public IGridResultsControlManager GetLastActiveGridControl();
    public IList<IGridResultsControlManager> GetAllGridControls();

    public Task ShowToolWindowAsync<T>(IToolWindowParams twParams = null) where T : IToolWindowCore;
    public Task CloseToolWindowAsync<T>() where T : IToolWindowCore;
}
