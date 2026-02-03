using SSMSMint.Core.Models;

namespace SSMSMint.Core.Interfaces;

public interface ISettingsManager
{
    public SSMSMintSettings GetSettings();
}
