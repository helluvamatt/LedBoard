using LedBoard.Models.Text;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace LedBoard.Services
{
	public static class FontService
	{
		private readonly static Dictionary<string, LedFont> _Fonts;
		private readonly static LedFont _DefaultFont;

		static FontService()
		{
			var fonts = (ResourceDictionary)Application.Current.Resources["ledFonts"];
			_Fonts = new Dictionary<string, LedFont>();
			foreach (DictionaryEntry entry in fonts)
			{
				if (entry.Value is LedFont font)
				{
					_Fonts.Add((string)entry.Key, font);
				}
			}
			_DefaultFont = GetFont((string)fonts["defaultFontKey"]);
		}

		public static LedFont GetDefault() => _DefaultFont;

		public static LedFont GetFont(string key) => _Fonts.ContainsKey(key) ? _Fonts[key] : null;

		public static IEnumerable<LedFont> GetFonts() => _Fonts.Values.OfType<LedFont>();
	}
}
