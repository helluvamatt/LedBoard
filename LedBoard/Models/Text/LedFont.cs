using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Resources;

namespace LedBoard.Models.Text
{
	public class LedFont
	{
		private readonly Dictionary<char, byte[]> _ColumnData;

		public LedFont()
		{
			_ColumnData = new Dictionary<char, byte[]>();
		}

		private Uri _Source;
		public Uri Source
		{
			get => _Source;
			set
			{
				_Source = value;
				StreamResourceInfo resource = Application.GetResourceStream(_Source);
				Load(resource.Stream);
			}
		}

		public string Name { get; private set; }
		public int FontHeight { get; private set; }
		public bool IsDoubleHeight { get; private set; }
		public int FixedWidth { get; private set; }

		public bool TryGetColumnData(char c, out byte[] data) => _ColumnData.TryGetValue(c, out data);

		private void Load(Stream stream)
		{
			var tokenRegex = new Regex(@"^\.(?<cmd>[A-Z_]+)(?:\s+(?<arg>.+))?$", RegexOptions.Compiled);
			using (var reader = new StreamReader(stream))
			{
				bool stop = false;
				string line, cmd, arg;
				char current = '\0';
				int currentWidth = 0;
				Match match;
				var definitions = new List<string>();
				while ((line = reader.ReadLine()) != null)
				{
					match = tokenRegex.Match(line);
					if (match.Success)
					{
						// Line is a command
						cmd = match.Groups["cmd"].Value;
						arg = match.Groups["arg"].Value;
						switch (cmd)
						{
							case "END":
								stop = true;
								break;
							case "NAME":
								Name = arg;
								break;
							case "FONT_HEIGHT":
								FontHeight = int.Parse(arg);
								break;
							case "HEIGHT":
								IsDoubleHeight = arg == "2";
								break;
							case "WIDTH":
								if (current == '\0') FixedWidth = int.Parse(arg);
								else currentWidth = int.Parse(arg);
								break;
							case "CHAR":
								if (current != '\0')
								{
									BuildChar(current, currentWidth, definitions.ToArray());
									currentWidth = 0;
									definitions.Clear();
								}
								current = (char)ushort.Parse(arg);
								break;
							case "NOTE":
								// Ignore character notes
								break;
							default:
								// Unknown command, ignore
								break;
						}
						if (stop) break;
					}
					else
					{
						// Line is a character definition
						definitions.Add(line);
					}
				}
			}
		}

		private void BuildChar(char c, int width, string[] definitions)
		{
			// Determine width
			if (width == 0) width = definitions.Max(s => s.Length);

			// build raw column data
			var columnData = new List<byte>(16);
			byte top, bottom;
			for (int x = 0; x < width; x++)
			{
				top = 0;
				bottom = 0;

				for (int y = 0; y < 8; y++)
				{
					if (y < definitions.Length && x < definitions[y].Length && definitions[y][x] == '*')
					{
						top |= (byte)(1 << y);
					}
					if (IsDoubleHeight && y + 8 < definitions.Length && x < definitions[y + 8].Length && definitions[y + 8][x] == '*')
					{
						bottom |= (byte)(1 << y);
					}
				}

				columnData.Add(top);
				if (IsDoubleHeight) columnData.Add(bottom);
			}

			_ColumnData.Add(c, columnData.ToArray());
		}

		public override int GetHashCode() => Source.GetHashCode();
		public override bool Equals(object obj) => obj is LedFont other && other.Source == Source;
		public override string ToString() => Name;
	}
}
