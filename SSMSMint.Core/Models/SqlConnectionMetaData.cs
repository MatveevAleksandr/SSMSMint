using static SSMSMint.Core.Models.SqlConnectionMetaData;

namespace SSMSMint.Core.Models;

public class SqlConnectionMetaData(string server, string database, AuthenticationType authType = AuthenticationType.WinAuth)
{
    public string ServerName { get; } = server;
    public string DatabaseName { get; } = database;
    public AuthenticationType AuthType { get; } = authType;
    public string AppName { get; } = Consts.AppName;

    public enum AuthenticationType
    {
        WinAuth = 0
    }
}
