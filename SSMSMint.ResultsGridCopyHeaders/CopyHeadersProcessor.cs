using Microsoft.SqlServer.Management.UI.Grid;
using SSMSMint.Shared.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace SSMSMint.ResultsGridCopyHeaders;

internal class CopyHeadersProcessor
{
    public void CopyHeadersToBuffer(bool onlySelected)
    {
        var gridControl = FrameService.GetLastFocusedOrFirstGridControl() ?? throw new Exception("Grid control is not found");

        var headers = onlySelected
            ? GetSelectedHeaders(gridControl)
            : GetAllHeaders(gridControl);

        Clipboard.SetText(string.Join(", ", headers));
    }

    private List<string> GetSelectedHeaders(IGridControl gridControl)
    {
        var headers = new List<string>();
        for (int i = 1; i < gridControl.ColumnsNumber; i++)
        {
            foreach (BlockOfCells cellBlock in gridControl.SelectedCells)
            {
                if (i >= cellBlock.X && i <= cellBlock.Right)
                {
                    gridControl.GetHeaderInfo(i, out var colHeader, out Bitmap _);
                    headers.Add(colHeader);
                    break;
                }
            }
        }
        return headers;
    }

    private List<string> GetAllHeaders(IGridControl gridControl)
    {
        var headers = new List<string>();
        for (int i = 1; i < gridControl.ColumnsNumber; i++)
        {
            gridControl.GetHeaderInfo(i, out var colHeader, out Bitmap _);
            headers.Add(colHeader);
        }
        return headers;
    }
}