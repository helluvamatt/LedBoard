using System;

namespace LedBoard.Models
{
	public interface IBoard
	{
		int Width { get; }
		int Height { get; }
		void BeginEdit();
		int this[int x, int y] { get; set; }
		void Copy(IBoard other);
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
			Copy(copy);
		}

		public int Width => _Leds.GetLength(1);
		public int Height => _Leds.GetLength(0);

		public void BeginEdit() { }

		public int this[int x, int y]
		{
			get => _Leds[y, x];
			set => _Leds[y, x] = value;
		}

		public void Copy(IBoard other)
		{
			if (other.Width != Width || other.Height != Height) throw new ArgumentException("Other board does not have same dimensions.");
			for (int y = 0; y < Height; y++)
			{
				for (int x = 0; x < Width; x++)
				{
					this[x, y] = other[x, y];
				}
			}
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
	}
}
