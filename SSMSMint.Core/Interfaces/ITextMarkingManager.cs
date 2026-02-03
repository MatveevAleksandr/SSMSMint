using SSMSMint.Core.Models;

namespace SSMSMint.Core.Interfaces;

public interface ITextMarkingManager
{
    public void ApplyMarkers(MarkersGroupDefinition markersGroup);
    public void ClearAllMarkers();
}
