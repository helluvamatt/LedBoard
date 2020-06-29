using System;
using System.Linq;

namespace LedBoard
{
	internal static class Utils
	{
        public static void HsvToRgb(double hue, double sat, double val, out byte red, out byte green, out byte blue)
        {
            while (hue < 0) hue += 360;
            while (hue >= 360) hue -= 360;
            double r, g, b;
            if (val <= 0) r = g = b = 0;
            else if (sat <= 0) r = g = b = val;
            else
            {
                double hf = Math.Floor(hue / 60.0);
                double f = hue / 60 - hf;
                int i = (int)hf % 6;

                double pv = val * (1 - sat);
                double qv = val * (1 - sat * f);
                double tv = val * (1 - sat * (1 - f));
                switch (i)
                {
                    case 0:
                        r = val;
                        g = tv;
                        b = pv;
                        break;
                    case 1:
                        r = qv;
                        g = val;
                        b = pv;
                        break;
                    case 2:
                        r = pv;
                        g = val;
                        b = tv;
                        break;
                    case 3:
                        r = pv;
                        g = qv;
                        b = val;
                        break;
                    case 4:
                        r = tv;
                        g = pv;
                        b = val;
                        break;
                    case 5:
                        r = val;
                        g = pv;
                        b = qv;
                        break;
                    default:
                        r = g = b = val;
                        break;
                }
            }
            red = (byte)(r * 255.0);
            green = (byte)(g * 255.0);
            blue = (byte)(b * 255.0);
        }

        public static void RgbToHsv(byte r, byte g, byte b, out double hue, out double sat, out double val)
		{
            byte minVal = Math.Min(Math.Min(r, g), b);
            byte maxVal = Math.Max(Math.Max(r, g), b);
            val = maxVal / 255.0;
            if (maxVal - minVal == 0)
			{
                hue = 0;
                sat = 0;
			}
            else
            {
                double max = maxVal / 255.0;
                double min = minVal / 255.0;
                double dR = r / 255.0;
                double dG = g / 255.0;
                double dB = b / 255.0;
                double delta = max - min;
                sat = delta / max;
                hue = 0;
                if (r == maxVal) hue = (dG - dB) / delta;
                else if (g == maxVal) hue = 2 + (dB - dR) / delta;
                else if (b == maxVal) hue = 4 + (dR - dG) / delta;
                hue *= 60;
                if (hue < 0.0f) hue += 360.0f;
            }
        }

        public static string TrimText(string str, int len)
        {
            return str.Length > len ? $"{str.Substring(0, len).TrimEnd()}..." : str;
        }

        public static bool In<T>(this T val, params T[] choices) where T : struct => choices.Contains(val);
    }
}
