using SSMSMint.Core.Interfaces;
using System.Collections.Generic;
using System.Windows.Forms;

namespace SSMSMint.Features;

public class CopyHeadersFeature
{
    public void CopyHeadersToClipboard(IGridResultsControlManager grManager, bool onlySelected)
    {
        var headers = new List<string>();

        if (onlySelected)
        {
            foreach (var col in grManager.GetSelectedColumnIndices())
            {
                headers.Add(grManager.GetColumnHeader(col));
            }
        }
        else
        {
            grManager.GetGridSize(out _, out var colCnt);
            for (var i = 1; i <= colCnt; i++)
            {
                headers.Add(grManager.GetColumnHeader(i));
            }
        }

        Clipboard.SetText(string.Join(", ", headers));
    }
}
