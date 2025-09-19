using Microsoft.SqlServer.Management.UI.Grid;
using System.Data;
using System.Reflection;

namespace SSMSMint.Shared.Extentions;

public static class GridStorageExtentions
{
    public static DataTable GetSchemaTable(this IGridStorage gridStorage) => gridStorage.GetType().GetField("m_schemaTable", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(gridStorage) as DataTable;
    public static string GetColumnHeader(this IGridStorage gridStorage, int colIndex) => gridStorage.GetSchemaTable().Rows[colIndex - 1][0]?.ToString();
}
