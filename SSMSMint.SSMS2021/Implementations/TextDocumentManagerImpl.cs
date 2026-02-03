using EnvDTE;
using SSMSMint.Core.Interfaces;
using SSMSMint.Core.Models;
using Microsoft.VisualStudio.Shell;
using System;
using System.Threading.Tasks;
using System.Windows.Threading;
using TextPoint = SSMSMint.Core.Models.TextPoint;

namespace SSMSMint.SSMS2021.Implementations;

internal class TextDocumentManagerImpl(TextDocument textDocument) : ITextDocumentManager
{
    public async Task<string> GetFullTextAsync()
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        var sp = new TextPoint(1, 1);
        var ep = new TextPoint(textDocument.EndPoint.Line, textDocument.EndPoint.LineCharOffset);
        return await GetTextAsync(new TextSpan(sp, ep));
    }

    public async Task<TextSpan> GetSelectionAsync()
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        var sel = textDocument.Selection;
        var sp = new TextPoint(sel.TopPoint.Line, sel.TopPoint.LineCharOffset);
        var ep = new TextPoint(sel.BottomPoint.Line, sel.BottomPoint.LineCharOffset);
        return new TextSpan(sp, ep);
    }

    public async Task<string> GetTextAsync(TextSpan ts)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        var sp = textDocument.StartPoint.CreateEditPoint();
        var ep = sp.CreateEditPoint();
        sp.MoveToLineAndOffset(ts.Start.Line, ts.Start.Column);
        ep.MoveToLineAndOffset(ts.End.Line, ts.End.Column);
        return sp.GetText(ep);
    }

    public async Task OutlineSectionAsync(TextSpan ts)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        var sp = textDocument.StartPoint.CreateEditPoint();
        var ep = sp.CreateEditPoint();
        sp.MoveToLineAndOffset(ts.Start.Line, ts.Start.Column);
        ep.MoveToLineAndOffset(ts.End.Line, ts.End.Column);
        sp.OutlineSection(ep);
    }

    public async Task SetSelectionAsync(TextSpan ts)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        Window editorWindow = textDocument.Parent.ActiveWindow;

        if (editorWindow == null)
        {
            return;
        }

        editorWindow.Activate();

        var selection = textDocument.Selection;
        selection.MoveToLineAndOffset(ts.Start.Line, ts.Start.Column);
        selection.MoveToLineAndOffset(ts.End.Line, ts.End.Column, true);

        // Тут нужно дать студии самой решить когда можно сделать фокус на окне.
        // Например если ставим выделение из кнопки какого нибудь ToolWindow, то фокус может тут же вернуться на то окно откуда нажали кнопку
        // Поэтому ставим фокус в очередь, чтобы студия сама решила когда это делать после всех обработок нажатий кнопок и тд
        await Dispatcher.CurrentDispatcher.BeginInvoke( // Так как мы на UI треде
            new Action(() =>
            {
                try
                {
                    editorWindow.SetFocus();
                }
                catch (Exception) { }
            }),
            DispatcherPriority.ApplicationIdle);
    }

    public async Task<int> GetColumnCountAsync(int line)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        var p = textDocument.StartPoint.CreateEditPoint();
        p.MoveToLineAndOffset(line, 1);
        p.EndOfLine();
        return p.LineCharOffset;
    }

    public async Task ReplaceTextAsync(TextSpan ts, string newText)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        var sp = textDocument.StartPoint.CreateEditPoint();
        var ep = sp.CreateEditPoint();
        sp.MoveToLineAndOffset(ts.Start.Line, ts.Start.Column);
        ep.MoveToLineAndOffset(ts.End.Line, ts.End.Column);
        sp.ReplaceText(ep, newText, 0);
    }

    public async Task<TextPoint> GetCaretPositionAsync()
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        return new TextPoint(textDocument.Selection.CurrentLine, textDocument.Selection.CurrentColumn);
    }
}
