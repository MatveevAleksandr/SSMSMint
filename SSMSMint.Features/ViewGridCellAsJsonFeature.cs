using SSMSMint.Core.Interfaces;
using SSMSMint.Core.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace SSMSMint.Features;

public class ViewGridCellAsJsonFeature(IWorkspaceManager workspaceManager)
{
    public async Task ProcessAsync(IGridResultsControlManager grManager)
    {
        string fileName;
        var pos = grManager.GetCurrentPosition();
        var colName = grManager.GetColumnHeader(pos.Column);
        var cellData = grManager.GetCellData(pos);

        if (string.IsNullOrWhiteSpace(colName))
        {
            fileName = "Undefined_JsonView.json";
        }
        else
        {
            fileName = $"{colName}_JsonView.json";
        }

        // Тут проверим на JSON ли. Если нет, то выбросит JsonReaderException
        var parsedJson = JToken.Parse(cellData);
        var formattedData = parsedJson.ToString(Formatting.Indented);

        // Отобразим отформатированный JSON
        var newTdManager = await workspaceManager.CreateNewFileAsync("General\\Text File", fileName);
        var span = new TextSpan(new TextPoint(1, 1), new TextPoint(1, 1));
        await newTdManager.ReplaceTextAsync(span, formattedData);
    }
}
