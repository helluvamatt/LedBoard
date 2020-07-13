using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace LedBoard.Converters
{
	public class BoolToVisibilityConverter : IValueConverter
	{
		public Visibility TrueVisibility { get; set; } = Visibility.Visible;
		public Visibility FalseVisibility { get; set; } = Visibility.Collapsed;

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is bool val)
			{
				return val ? TrueVisibility : FalseVisibility;
			}
			return Binding.DoNothing;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
