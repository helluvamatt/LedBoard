using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace LedBoard.Converters
{
	public class ColorConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is int color)
			{
				byte r = (byte)((color >> 16) & 0xFF);
				byte g = (byte)((color >> 8) & 0xFF);
				byte b = (byte)(color & 0xFF);
				return Color.FromRgb(r, g, b);
			}
			return Binding.DoNothing;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is Color color)
			{
				return color.R << 16 | color.G << 8 | color.B;
			}
			return Binding.DoNothing;
		}
	}
}
