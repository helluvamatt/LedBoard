using LedBoard.Interop;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace LedBoard.Converters
{
	public class ExtensionIconConverter : IValueConverter
	{
		public int IconWidth { get; set; } = 32;
		public int IconHeight { get; set; } = 32;

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is string filePath)
			{
				IntPtr hIcon = Shell32.GetFileTypeIcon(filePath);
				if (hIcon != IntPtr.Zero)
				{
					return Imaging.CreateBitmapSourceFromHIcon(hIcon, Int32Rect.Empty, BitmapSizeOptions.FromWidthAndHeight(IconWidth, IconHeight));
				}
			}
			return Binding.DoNothing;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
