using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace LedBoard.Converters
{
	public class TimelineWidthConverter : IMultiValueConverter
	{
		public bool ReturnThickness { get; set; }

		public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			if (values[0] is double time && values[1] is double zoomFactor)
			{
				double value = time * zoomFactor;
				if (parameter is double extra) value += extra;
				if (parameter is string extraStr && double.TryParse(extraStr, out double extraFromStr)) value += extraFromStr;
				if (ReturnThickness) return new Thickness(value, 0, 0, 0);
				return value;
			}
			return Binding.DoNothing;
		}

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
