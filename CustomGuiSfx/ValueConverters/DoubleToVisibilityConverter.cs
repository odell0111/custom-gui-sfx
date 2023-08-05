using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CustomGuiSfx.ViewModel.ValueConverters;

public class DoubleToVisibilityConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => ((double)value).Equals(0.0) ? Visibility.Collapsed : Visibility.Visible;

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => value.Equals(Visibility.Visible) ? 1 : 0;
}
