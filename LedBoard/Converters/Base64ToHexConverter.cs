using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace LedBoard.Converters
{
	public class Base64ToHexConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is string base64data)
			{
				byte[] rawData = System.Convert.FromBase64String(base64data);
				StringBuilder hex = new StringBuilder(rawData.Length * 2);
				foreach (byte b in rawData)
				{
					hex.AppendFormat("{0:X2}", b);
				}
				return hex.ToString();
			}
			return value;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is string hexData)
			{
				int len = hexData.Length;
				byte[] rawData = new byte[len / 2];
				for (int i = 0; i < len; i += 2)
				{
					rawData[i / 2] = System.Convert.ToByte(hexData.Substring(i, 2), 16);
				}
				return System.Convert.ToBase64String(rawData);
			}
			return value;
		}
	}
}
