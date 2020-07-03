using LedBoard.Models;
using LedBoard.Services.Rendering;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace LedBoard.Controls
{
	public class LedBoardControl : Image
	{
		private readonly BoardRenderer _Renderer;

		private WriteableBitmap _Bitmap;

		public LedBoardControl()
		{
			_Renderer = new BoardRenderer();
		}

		#region Dependency properties

		#region IsReadOnlyMode

		public static readonly DependencyProperty IsReadOnlyModeProperty = DependencyProperty.Register(nameof(IsReadOnlyMode), typeof(bool), typeof(LedBoardControl), new PropertyMetadata(true));

		public bool IsReadOnlyMode
		{
			get => (bool)GetValue(IsReadOnlyModeProperty);
			set => SetValue(IsReadOnlyModeProperty, value);
		}

		#endregion

		#region CurrentBoard

		public static readonly DependencyProperty CurrentBoardProperty = DependencyProperty.Register(nameof(CurrentBoard), typeof(IBoard), typeof(LedBoardControl), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure, OnBoardChanged));

		public IBoard CurrentBoard
		{
			get => (IBoard)GetValue(CurrentBoardProperty);
			set => SetValue(CurrentBoardProperty, value);
		}

		#endregion

		#region DotPitch

		public static readonly DependencyProperty DotPitchProperty = DependencyProperty.Register(nameof(DotPitch), typeof(int), typeof(LedBoardControl), new PropertyMetadata(2, OnPropertyChanged));

		public int DotPitch
		{
			get => (int)GetValue(DotPitchProperty);
			set => SetValue(DotPitchProperty, value);
		}

		#endregion

		#region PixelSize

		public static readonly DependencyProperty PixelSizeProperty = DependencyProperty.Register(nameof(PixelSize), typeof(int), typeof(LedBoardControl), new PropertyMetadata(5, OnPropertyChanged));

		public int PixelSize
		{
			get => (int)GetValue(PixelSizeProperty);
			set => SetValue(PixelSizeProperty, value);
		}

		#endregion

		#region Zoom

		public static readonly DependencyProperty ZoomProperty = DependencyProperty.Register(nameof(Zoom), typeof(double), typeof(LedBoardControl), new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

		public double Zoom
		{
			get => (double)GetValue(ZoomProperty);
			set => SetValue(ZoomProperty, value);
		}

		#endregion

		#region ZoomMode

		public static readonly DependencyProperty ZoomModeProperty = DependencyProperty.Register(nameof(ZoomMode), typeof(LedBoardZoomMode), typeof(LedBoardControl), new FrameworkPropertyMetadata(LedBoardZoomMode.FitContentToSize, FrameworkPropertyMetadataOptions.AffectsMeasure));

		public LedBoardZoomMode ZoomMode
		{
			get => (LedBoardZoomMode)GetValue(ZoomModeProperty);
			set => SetValue(ZoomModeProperty, value);
		}

		#endregion

		#endregion

		protected override Size MeasureOverride(Size constraint)
		{
			if (ZoomMode == LedBoardZoomMode.FitSizeToContent)
			{
				return GetScaledSize();
			}
			return base.MeasureOverride(constraint);
		}

		protected override Size ArrangeOverride(Size arrangeSize)
		{
			if (ZoomMode == LedBoardZoomMode.FitSizeToContent)
			{
				arrangeSize = GetScaledSize();
			}
			return base.ArrangeOverride(arrangeSize);
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
				int imageWidth = _Renderer.GetPixelLength(CurrentBoard.Width, DotPitch, PixelSize);
				int imageHeight = _Renderer.GetPixelLength(CurrentBoard.Height, DotPitch, PixelSize);
				if (_Bitmap == null || _Bitmap.PixelWidth != imageWidth || _Bitmap.PixelHeight != imageHeight)
				{
					Source = _Bitmap = _Renderer.CreateWriteableBitmap(CurrentBoard.Width, CurrentBoard.Height, DotPitch, PixelSize);
				}
				_Renderer.RenderBoard(CurrentBoard, _Bitmap, DotPitch, PixelSize);
			}
			else
			{
				Source = null;
			}
		}

		private Size GetScaledSize()
		{
			return new Size((Source?.Width ?? 0) * Zoom, (Source?.Height ?? 0) * Zoom);
		}
	}

	public enum LedBoardZoomMode { FitContentToSize, FitSizeToContent }
}
