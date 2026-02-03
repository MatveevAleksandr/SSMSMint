using System;
using System.Globalization;
using System.Windows.Data;

namespace SSMSMint.Core.UI.Converters;

internal class RowIndexToDisplayConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return $"Row: {(long)value + 1}";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
