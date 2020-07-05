using System;

namespace LedBoard.Models
{
	public interface IBoard
	{
		int Width { get; }
		int Height { get; }
		void BeginEdit();
		int this[int x, int y] { get; set; }
		void Commit(object committer);

		event EventHandler BoardChanged;
	}

	public class MemoryBoard : IBoard
	{
		private readonly int[,] _Leds;

		public MemoryBoard(int width, int height)
		{
			_Leds = new int[height, width];
		}

		public MemoryBoard(IBoard copy)
		{
			_Leds = new int[copy.Height, copy.Width];
			this.CopyFrom(copy);
		}

		public int Width => _Leds.GetLength(1);
		public int Height => _Leds.GetLength(0);

		public void BeginEdit() { }

		public int this[int x, int y]
		{
			get => _Leds[y, x];
			set => _Leds[y, x] = value;
		}

		public void Commit(object committer)
		{
			BoardChanged?.Invoke(committer, EventArgs.Empty);
		}

		public event EventHandler BoardChanged;
	}

	public static class BoardExtensions
	{
		public static void SetAll(this IBoard board, int color)
		{
			for (int y = 0; y < board.Height; y++)
			{
				for (int x = 0; x < board.Width; x++)
				{
					board[x, y] = color;
				}
			}
		}

		public static void CopyFrom(this IBoard destination, IBoard source)
		{
			if (source.Width != destination.Width || source.Height != destination.Height) throw new ArgumentException("Other board does not have same dimensions.");
			for (int y = 0; y < destination.Height; y++)
			{
				for (int x = 0; x < destination.Width; x++)
				{
					destination[x, y] = source[x, y];
				}
			}
		}

		public static void CopySubset(this IBoard source, IBoard destination, int srcX, int srcY, int srcW, int srcH, int dstX, int dstY)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (destination == null) throw new ArgumentNullException(nameof(destination));
			int sourceWidth = source.Width;
			int sourceHeight = source.Height;
			int destWidth = destination.Width;
			int destHeight = destination.Height;
			for (int y = srcY, dy = dstY; y < srcY + srcH && dy < dstY + srcH && y < sourceHeight && dy < destHeight; y++, dy++)
			{
				for (int x = srcX, dx = dstX; x < srcX + srcW && dx < dstX + srcW && x < sourceWidth && dx < destWidth; x++, dx++)
				{
					destination[dx, dy] = source[x, y];
				}
			}
		}
	}
}
