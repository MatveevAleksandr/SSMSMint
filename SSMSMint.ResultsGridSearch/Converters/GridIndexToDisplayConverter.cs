﻿using System;
using System.Globalization;
using System.Windows.Data;

namespace SSMSMint.ResultsGridSearch.Converters;

internal class GridIndexToDisplayConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return $"Grid: {(int)value + 1}";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
