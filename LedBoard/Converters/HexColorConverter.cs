using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace LedBoard.Converters
{
	public class HexColorConverter : IValueConverter
	{
		public bool IncludeAlpha { get; set; }

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is string val)
			{
				Color color = (Color)System.Windows.Media.ColorConverter.ConvertFromString(val);
				if (!IncludeAlpha) color.A = 0xFF;
				return color;
			}
			else if (value is Color c)
			{

				return IncludeAlpha ? $"#{c.A:X2}{c.R:X2}{c.G:X2}{c.B:X2}" : $"#{c.R:X2}{c.G:X2}{c.B:X2}";
			}
			return Binding.DoNothing;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Convert(value, targetType, parameter, culture);
	}
}
