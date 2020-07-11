using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;

namespace LedBoard.Converters
{
	public class FileNameConverter : IValueConverter
	{
		public FileNameConverterMode Mode { get; set; } = FileNameConverterMode.None;

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is string fullPath)
			{
				switch (Mode)
				{
					case FileNameConverterMode.Directory:
						return Path.GetDirectoryName(fullPath);
					case FileNameConverterMode.FileName:
						return Path.GetFileName(fullPath);
					case FileNameConverterMode.Extension:
						string extension = Path.GetExtension(fullPath);
						if (!string.IsNullOrWhiteSpace(extension)) extension = extension.Substring(1);
						return extension;
					case FileNameConverterMode.FileNameWithoutExtension:
						return Path.GetFileNameWithoutExtension(fullPath);
					case FileNameConverterMode.None:
					default:
						return fullPath;
				}
			}
			return Binding.DoNothing;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

	public enum FileNameConverterMode { None, FileName, Directory, Extension, FileNameWithoutExtension }
}
