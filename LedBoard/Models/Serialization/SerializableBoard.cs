using System;
using System.Collections.Generic;

namespace LedBoard.Models.Serialization
{
	public class SerializableBoard : IBoard
	{
		private readonly int[,] _Board;

		private byte[] _Serialized;

		public SerializableBoard(int width, int height)
		{
			if (width > ushort.MaxValue || height > ushort.MaxValue) throw new NotSupportedException($"SerializableBoard does not support boards larger than {ushort.MaxValue} x {ushort.MaxValue}");
			Width = width;
			Height = height;
			_Board = new int[Height, Width];
			Serialize();
		}

		public SerializableBoard(byte[] data)
		{
			if (_Serialized == null) throw new ArgumentNullException(nameof(data));
			if (_Serialized.Length < 4) throw new ArgumentException("Input data is too short.");
			_Serialized = data;
			// Header: 4 bytes
			// - Width: 2 bytes (LE)
			// - Height: 2 bytes (LE)
			Width = (_Serialized[0] << 8) | _Serialized[1];
			Height = (_Serialized[2] << 8) | _Serialized[3];
			_Board = new int[Width, Height];
			int offset = 4;
			if (_Serialized.Length != (Width * Height * 3) + 4) throw new ArgumentException("Input data is not the correct length.");
			for (int y = 0; y < Height; y++)
			{
				for (int x = 0; x < Width; x++)
				{
					this[x, y] = (_Serialized[offset] << 16) // Red
						| (_Serialized[offset + 1] << 8) // Green
						| (_Serialized[offset + 2]); // Blue
					offset += 3;
				}
			}
		}

		public int this[int x, int y]
		{
			get => _Board[y, x];
			set => _Board[y, x] = value;
		}

		public int Width { get; }
		public int Height { get; }

		public event EventHandler BoardChanged;

		public void BeginEdit() { }

		public void Commit(object committer)
		{
			Serialize();
			BoardChanged?.Invoke(committer, EventArgs.Empty);
		}

		public IEnumerable<byte> Serialized => _Serialized;

		private void Serialize()
		{
			byte[] buffer = new byte[Width * Height * 3 + 4];
			// Header: 4 bytes
			// - Width: 2 bytes (LE)
			// - Height: 2 bytes (LE)
			buffer[0] = (byte)((Width >> 8) & 0xFF);
			buffer[1] = (byte)(Width & 0xFF);
			buffer[2] = (byte)((Height >> 8) & 0xFF);
			buffer[3] = (byte)(Height & 0xFF);
			int offset = 4;
			for (int y = 0; y < Height; y++)
			{
				for (int x = 0; x < Width; x++)
				{
					buffer[offset] = (byte)((this[x, y] >> 16) & 0xFF); // Red
					buffer[offset + 1] = (byte)((this[x, y] >> 8) & 0xFF); // Green
					buffer[offset + 2] = (byte)(this[x, y] & 0xFF); // Blue
					offset += 3;
				}
			}
			_Serialized = buffer;
		}
	}
}
