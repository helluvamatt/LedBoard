using LedBoard.Interop;
using System;
using System.Globalization;
using System.Windows.Data;

namespace LedBoard.Converters
{
	public class ExtensionDescriptionConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is string filePath)
			{
				return Shell32.GetFileTypeDescription(filePath);
			}
			return Binding.DoNothing;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
