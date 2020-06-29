using LedBoard.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace LedBoard
{
	public class LedBoardControl : Image
	{
		private WriteableBitmap _Bitmap;

		public static readonly DependencyProperty IsReadOnlyModeProperty = DependencyProperty.Register(nameof(IsReadOnlyMode), typeof(bool), typeof(LedBoardControl), new PropertyMetadata(true));

		public bool IsReadOnlyMode
		{
			get => (bool)GetValue(IsReadOnlyModeProperty);
			set => SetValue(IsReadOnlyModeProperty, value);
		}

		public static readonly DependencyProperty CurrentBoardProperty = DependencyProperty.Register(nameof(CurrentBoard), typeof(IBoard), typeof(LedBoardControl), new PropertyMetadata(null, OnBoardChanged));

		public IBoard CurrentBoard
		{
			get => (IBoard)GetValue(CurrentBoardProperty);
			set => SetValue(CurrentBoardProperty, value);
		}

		public static readonly DependencyProperty DotPitchProperty = DependencyProperty.Register(nameof(DotPitch), typeof(int), typeof(LedBoardControl), new PropertyMetadata(2, OnPropertyChanged));

		public int DotPitch
		{
			get => (int)GetValue(DotPitchProperty);
			set => SetValue(DotPitchProperty, value);
		}

		public static readonly DependencyProperty PixelSizeProperty = DependencyProperty.Register(nameof(PixelSize), typeof(int), typeof(LedBoardControl), new PropertyMetadata(5, OnPropertyChanged));

		public int PixelSize
		{
			get => (int)GetValue(PixelSizeProperty);
			set => SetValue(PixelSizeProperty, value);
		}

		protected override void OnMouseDown(MouseButtonEventArgs e)
		{
			if (IsReadOnlyMode || CurrentBoard == null) return;
			EditPoint(e);
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			if (IsReadOnlyMode || CurrentBoard == null) return;
			EditPoint(e);
		}

		private void EditPoint(MouseEventArgs e)
		{
			e.Handled = true;
			Point p = e.GetPosition(this);
			int color;
			if (e.LeftButton == MouseButtonState.Pressed) color = 0xFFFFFF;
			else if (e.RightButton == MouseButtonState.Pressed) color = 0x000000;
			else return;
			double cellSize = Math.Min(ActualWidth / CurrentBoard.Width, ActualHeight / CurrentBoard.Height);
			var origin = new Point((ActualWidth - CurrentBoard.Width * cellSize) / 2, (ActualHeight - CurrentBoard.Height * cellSize) / 2);
			int x = (int)Math.Floor((p.X - origin.X) / cellSize);
			int y = (int)Math.Floor((p.Y - origin.Y) / cellSize);
			if (x >= 0 && x < CurrentBoard.Width && y >= 0 && y < CurrentBoard.Height)
			{
				CurrentBoard.BeginEdit();
				CurrentBoard[x, y] = color;
				CurrentBoard.Commit(this);
			}
			UpdateImage();
		}

		private static void OnBoardChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
		{
			var control = (LedBoardControl)sender;
			if (e.OldValue is IBoard oldBoard) oldBoard.BoardChanged -= control.OnBoardChanged;
			if (e.NewValue is IBoard newBoard) newBoard.BoardChanged += control.OnBoardChanged;
			control.UpdateImage();
		}

		private static void OnPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
		{
			var control = (LedBoardControl)sender;
			control.UpdateImage();
		}

		private void OnBoardChanged(object sender, EventArgs e)
		{
			Dispatcher.Invoke(UpdateImage);
		}

		private void UpdateImage()
		{
			if (CurrentBoard != null)
			{
				int imageWidth = CurrentBoard.Width * (PixelSize + DotPitch) + DotPitch;
				int imageHeight = CurrentBoard.Height * (PixelSize + DotPitch) + DotPitch;
				if (_Bitmap == null || _Bitmap.PixelWidth != imageWidth || _Bitmap.PixelHeight != imageHeight)
				{
					Source = _Bitmap = new WriteableBitmap(imageWidth, imageHeight, 96, 96, PixelFormats.Bgr24, null);
				}
				Render();
			}
			else
			{
				Source = null;
			}
		}

		#region Rendering

		private byte[] _Buffer;
		private int _CachedImageWidth;
		private int _CachedImageHeight;
		private int _CachedStride;
		private int _Pixel;

		private void Render()
		{
			int boardWidth = CurrentBoard.Width;
			int boardHeight = CurrentBoard.Height;
			int pixelSize = PixelSize;
			int dotPitch = DotPitch;
			int imageWidth = boardWidth * (pixelSize + dotPitch) + dotPitch;
			int imageHeight = boardHeight * (pixelSize + dotPitch) + dotPitch;
			if (_Buffer == null || _CachedImageWidth != imageWidth || _CachedImageHeight != imageHeight)
			{
				_Buffer = new byte[imageWidth * imageHeight * 3];
				_CachedImageWidth = imageWidth;
				_CachedImageHeight = imageHeight;
				_CachedStride = _CachedImageWidth * 3;
			}
			int baseX, baseY, x, y;
			for (int boardY = 0; boardY < boardHeight; boardY++)
			{
				baseY = boardY * (pixelSize + dotPitch) + dotPitch;
				for (int boardX = 0; boardX < boardWidth; boardX++)
				{
					baseX = boardX * (pixelSize + dotPitch) + dotPitch;
					_Pixel = CurrentBoard[boardX, boardY];
					for (int pixelY = 0; pixelY < pixelSize; pixelY++)
					{
						y = baseY + pixelY;
						for (int pixelX = 0; pixelX < pixelSize; pixelX++)
						{
							x = baseX + pixelX;
							if (PixelSize > 2 && (pixelX == 0 || pixelX == pixelSize - 1) && (pixelY == 0 || pixelY == pixelSize - 1)) continue;
							//if (PixelSize > 6) // TODO Logic for more rounded hi-res pixels
							_Buffer[y * _CachedStride + x * 3 + 0] = (byte)((_Pixel >> 0) & 0xFF); // B
							_Buffer[y * _CachedStride + x * 3 + 1] = (byte)((_Pixel >> 8) & 0xFF); // G
							_Buffer[y * _CachedStride + x * 3 + 2] = (byte)((_Pixel >> 16) & 0xFF); // R
						}
					}
				}
			}
			_Bitmap.WritePixels(new Int32Rect(0, 0, _CachedImageWidth, _CachedImageHeight), _Buffer, _CachedStride, 0, 0);
		}

		#endregion
	}
}
