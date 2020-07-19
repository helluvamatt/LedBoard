using System;
using System.Globalization;
using System.Windows.Data;

namespace LedBoard.Converters
{
	public class FileSizeConverter : IValueConverter
	{
		private static readonly string[] SIZES = { "bytes", "KB", "MB", "GB", "TB" };

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is long filesize)
			{
				int order = 0;
				double len = filesize;
				while (len >= 1024 && order < SIZES.Length - 1)
				{
					order++;
					len /= 1024;
				}
				return string.Format(culture, "{0:0.##} {1}", len, SIZES[order]);
			}
			return Binding.DoNothing;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
