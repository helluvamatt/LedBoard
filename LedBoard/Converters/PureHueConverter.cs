using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace LedBoard.Converters
{
	public class PureHueConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is Color baseColor)
			{
				Utils.RgbToHsv(baseColor.R, baseColor.G, baseColor.B, out double h, out _, out _);
				value = h;
			}
			if (value is double hue)
			{
				Utils.HsvToRgb(hue, 1.0, 1.0, out byte r, out byte g, out byte b);
				return Color.FromRgb(r, g, b);
			}
			return Binding.DoNothing;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
