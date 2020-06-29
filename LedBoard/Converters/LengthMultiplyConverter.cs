using System;
using System.Globalization;
using System.Windows.Data;

namespace LedBoard.Converters
{
	public class LengthMultiplyConverter : IMultiValueConverter
	{
		public bool Invert { get; set; }

		public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			if (values[0] is double length && values[1] is double factor)
			{
				double value = length * factor;
				if (Invert) value = length - value;
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
