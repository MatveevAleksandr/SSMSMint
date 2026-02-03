using SSMSMint.Core.Interfaces;
using SSMSMint.Core.Models;
using Microsoft.SqlServer.Management.UI.Grid;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace SSMSMint.SSMS2021.Implementations;

internal class GridResultsControlManagerImpl(IGridControl gridControl) : IGridResultsControlManager
{
    private readonly IGridControl _gridControl = gridControl;

    public IReadOnlyList<int> GetSelectedColumnIndices()
    {
        var res = new List<int>();
        for (int i = 1; i < _gridControl.ColumnsNumber; i++)
        {
            foreach (BlockOfCells cellBlock in _gridControl.SelectedCells)
            {
                if (i >= cellBlock.X && i <= cellBlock.Right)
                {
                    res.Add(i);
                    break;
                }
            }
        }
        return res;
    }

    public string GetColumnHeader(int index)
    {
        _gridControl.GetHeaderInfo(index, out var colHeader, out Bitmap _);
        return colHeader;
    }

    public string GetCellData(GridCellPoint gcPoint)
    {
        return _gridControl.GridStorage.GetCellDataAsString(gcPoint.Row, gcPoint.Column);
    }

    public GridCellPoint GetCurrentPosition()
    {
        _gridControl.GetCurrentCell(out var row, out var col);
        return new GridCellPoint(row, col);
    }

    public void GetGridSize(out long rowCnt, out int colCnt)
    {
        var gridStorage = _gridControl.GridStorage;
        rowCnt = gridStorage.NumRows();
        colCnt = _gridControl.ColumnsNumber - 1;
    }

    public async Task FocusCellAsync(GridCellPoint gcPoint)
    {
        var selectionManager = _gridControl.GetType().GetField("m_selMgr", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(_gridControl) as SelectionManager; ;
        selectionManager.Clear(); // Очистим все прочие selection
        selectionManager.StartNewBlock(gcPoint.Row, gcPoint.Column); // Выделим найденную ячейку
        _gridControl.EnsureCellIsVisible(gcPoint.Row, gcPoint.Column); // Подвинем скролл чтоб было видно выделенную ячейку

        await Dispatcher.CurrentDispatcher.BeginInvoke(
            new Action(() =>
            {
                try
                {
                    (_gridControl as GridControl).Focus();
                }
                catch (Exception) { }
            }),
            DispatcherPriority.ApplicationIdle);
    }




    public bool Equals(GridResultsControlManagerImpl other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }
        // экземпляры-обертки равны, если они ссылаются на один и тот же внутренний gridControl
        return ReferenceEquals(_gridControl, other._gridControl);
    }

    public override bool Equals(object obj) => Equals(obj as GridResultsControlManagerImpl);

    public override int GetHashCode() => RuntimeHelpers.GetHashCode(_gridControl);
}
