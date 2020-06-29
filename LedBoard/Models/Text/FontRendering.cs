using System;
using System.Collections.Generic;

namespace LedBoard.Models.Text
{
	internal class FontRendering
	{
		private readonly List<byte> _ColumnDataTop;
		private readonly List<byte> _ColumnDataBottom;
		private LedFont _Font;

		public FontRendering()
		{
			_ColumnDataTop = new List<byte>();
			_ColumnDataBottom = new List<byte>();
		}

		public int FontHeight => _Font != null ? (_Font.IsDoubleHeight ? (_Font.FontHeight * 2) : _Font.FontHeight) : 0;
		public int TextWidth => Math.Max(_ColumnDataTop.Count, _ColumnDataBottom.Count);

		public void Layout(LedFont font, string text)
		{
			_Font = font ?? throw new ArgumentNullException(nameof(font));

			// Build column data
			_ColumnDataTop.Clear();
			_ColumnDataBottom.Clear();
			foreach (char c in text)
			{
				if (_Font.TryGetColumnData(c, out byte[] colData))
				{
					if (_Font.IsDoubleHeight)
					{
						for (int i = 0; i < colData.Length; i += 2)
						{
							_ColumnDataTop.Add(colData[i]);
							_ColumnDataBottom.Add(colData[i + 1]);
						}
					}
					else
					{
						_ColumnDataTop.AddRange(colData);
					}
					_ColumnDataTop.Add(0);
					if (_Font.IsDoubleHeight) _ColumnDataBottom.Add(0);
				}
			}
		}

		public void RenderText(IBoard board, int x, int y, int bgColor, int fgColor)
		{
			board.SetAll(bgColor);

			if (_Font == null) return;

			int col;
			for (int px = Math.Max(x, 0); px < board.Width; px++)
			{
				col = px - x;

				// No more text columns, break
				if (col >= _ColumnDataTop.Count || (_Font.IsDoubleHeight && col >= _ColumnDataBottom.Count)) break;

				// Set column data
				for (int py = 0; py < _Font.FontHeight; py++)
				{
					if (py + y < 0 || py + y >= board.Height) continue;
					if (((_ColumnDataTop[col] >> py) & 0x1) == 1) board[px, py + y] = fgColor;
				}
				if (_Font.IsDoubleHeight)
				{
					for (int py = 0; py < _Font.FontHeight; py++)
					{
						if (py + y + _Font.FontHeight < 0 || py + y + _Font.FontHeight >= board.Height) continue;
						if (((_ColumnDataBottom[col] >> py) & 0x1) == 1) board[px, py + y + _Font.FontHeight] = fgColor;
					}
				}
			}
		}
	}
}
