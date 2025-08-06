using EnvDTE;
using Microsoft.VisualStudio.Shell;
using System.Collections.Generic;

namespace SSMSMint.Regions
{
    public static class TextDocumentExtentions
    {
        public static void CreateCustomRegions(this TextDocument document)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var startRegionsPoints = new Stack<EditPoint>();
            var searchPoint = document.StartPoint.CreateEditPoint();

            while (!searchPoint.AtEndOfDocument)
            {
                string line = searchPoint.GetLines(searchPoint.Line, searchPoint.Line + 1).Trim();

                if (line.StartsWith("--#region"))
                {
                    startRegionsPoints.Push(searchPoint.CreateEditPoint());
                }
                else if (line.StartsWith("--#endregion"))
                {
                    // Случай если окончаний региона больше, чем начал. Такое окончание проигнорируем
                    if (startRegionsPoints.Count != 0)
                    {
                        var startPoint = startRegionsPoints.Pop();
                        var endPoint = searchPoint.CreateEditPoint();
                        startPoint.EndOfLine(); // Что бы было видно название региона
                        endPoint.EndOfLine(); // Что бы не было видно тега конца региона
                        startPoint.OutlineSection(endPoint);
                    }
                }
                searchPoint.LineDown();
            }
        }
    }
}
