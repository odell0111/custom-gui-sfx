using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CustomGuiSfx.ViewModel.ValueConverters;

public class StringToVisibilityConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => string.IsNullOrWhiteSpace((string)value) ? Visibility.Collapsed : Visibility.Visible;

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => value.Equals(Visibility.Visible) ? "Visible" : "Collapsed";
}
