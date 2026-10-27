using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace NeeView
{
    public class PageSelectDialogValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is string text && PageSelectDialogModel.TryParseValue(text, out var result)
                ? result
                : DependencyProperty.UnsetValue;
        }
    }
}
