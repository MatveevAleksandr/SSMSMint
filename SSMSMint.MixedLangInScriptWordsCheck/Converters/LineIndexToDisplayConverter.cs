using System;
using System.Globalization;
using System.Windows.Data;

namespace SSMSMint.MixedLangInScriptWordsCheck.Converters;

class LineIndexToDisplayConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return $"Line: {(int)value + 1}";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
