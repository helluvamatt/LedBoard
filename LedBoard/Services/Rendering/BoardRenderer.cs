using LedBoard.Models;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace LedBoard.Services.Rendering
{
	public class BoardRenderer
	{
		private byte[] _Buffer;
		private int _CachedImageWidth;
		private int _CachedImageHeight;
		private int _CachedStride;

		public void RenderBoard(IBoard board, WriteableBitmap bitmap, int dotPitch, int pixelSize)
		{
			int imageWidth = GetPixelLength(board.Width, dotPitch, pixelSize);
			int imageHeight = GetPixelLength(board.Height, dotPitch, pixelSize);
			if (_Buffer == null || _CachedImageWidth != imageWidth || _CachedImageHeight != imageHeight)
			{
				_Buffer = new byte[imageWidth * imageHeight * 3];
				_CachedImageWidth = imageWidth;
				_CachedImageHeight = imageHeight;
				_CachedStride = _CachedImageWidth * 3;
			}
			int boardWidth = board.Width;
			int boardHeight = board.Height;
			int baseX, baseY, x, y, pixel;
			for (int boardY = 0; boardY < boardHeight; boardY++)
			{
				baseY = GetPixelLength(boardY, dotPitch, pixelSize);
				for (int boardX = 0; boardX < boardWidth; boardX++)
				{
					baseX = GetPixelLength(boardX, dotPitch, pixelSize);
					pixel = board[boardX, boardY];
					for (int pixelY = 0; pixelY < pixelSize; pixelY++)
					{
						y = baseY + pixelY;
						for (int pixelX = 0; pixelX < pixelSize; pixelX++)
						{
							x = baseX + pixelX;
							// Pixel Size: > 4: 1 pixel notch
							if (pixelSize > 4 && (pixelX == 0 || pixelX == pixelSize - 1) && (pixelY == 0 || pixelY == pixelSize - 1)) continue;
							// Pixel Size: > 9: 2x2 pixel notch + 1x2 pixel notch
							if (pixelSize > 9 && (pixelX <= 1 || pixelX >= pixelSize - 2) && (pixelY <= 1 || pixelY >= pixelSize - 2)) continue;
							if (pixelSize > 9 && (pixelX == 0 || pixelX == pixelSize - 1) && (pixelY <= 3 || pixelY >= pixelSize - 4)) continue;
							if (pixelSize > 9 && (pixelX <= 3 || pixelX >= pixelSize - 4) && (pixelY == 0 || pixelY == pixelSize - 1)) continue;
							// Set pixel
							_Buffer[y * _CachedStride + x * 3 + 0] = (byte)((pixel >> 0) & 0xFF); // B
							_Buffer[y * _CachedStride + x * 3 + 1] = (byte)((pixel >> 8) & 0xFF); // G
							_Buffer[y * _CachedStride + x * 3 + 2] = (byte)((pixel >> 16) & 0xFF); // R
						}
					}
				}
			}
			bitmap.WritePixels(new Int32Rect(0, 0, _CachedImageWidth, _CachedImageHeight), _Buffer, _CachedStride, 0, 0);
		}

		public WriteableBitmap CreateWriteableBitmap(int boardWidth, int boardHeight, int dotPitch, int pixelSize)
		{
			int imageWidth = GetPixelLength(boardWidth, dotPitch, pixelSize);
			int imageHeight = GetPixelLength(boardHeight, dotPitch, pixelSize);
			return new WriteableBitmap(imageWidth, imageHeight, 96, 96, PixelFormats.Bgr24, null);
		}

		public int GetPixelLength(int pixelCount, int dotPitch, int pixelSize)
		{
			return pixelCount * (pixelSize + dotPitch) + dotPitch;
		}
	}
}
