using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace LedBoard.Converters
{
	public class ByteToBrushConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is byte component)
			{
				return new SolidColorBrush(Color.FromArgb(0xFF, component, component, component));
			}
			return Binding.DoNothing;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is SolidColorBrush brush)
			{
				return brush.Color.R;
			}
			return Binding.DoNothing;
		}
	}
}
