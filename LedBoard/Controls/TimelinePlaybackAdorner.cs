using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace LedBoard.Controls
{
	public class TimelinePlaybackAdorner : Adorner
	{
		private Pen _Pen;

		public TimelinePlaybackAdorner(TimelineControl adornedElement) : base(adornedElement)
		{
			SnapsToDevicePixels = true;
		}

		public static readonly DependencyProperty OffsetXProperty = DependencyProperty.Register(nameof(OffsetX), typeof(double), typeof(TimelinePlaybackAdorner), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

		public double OffsetX
		{
			get => (double)GetValue(OffsetXProperty);
			set => SetValue(OffsetXProperty, value);
		}

		public Color Color
		{
			set
			{
				_Pen = new Pen(new SolidColorBrush(value), 1);
				InvalidateVisual();
			}
		}

		protected override void OnRender(DrawingContext drawingContext)
		{
			drawingContext.PushTransform(new TranslateTransform(Math.Round(OffsetX), -16));
			drawingContext.DrawGeometry(null, _Pen, new LineGeometry(new Point(0, 0), new Point(0, Height + 16)));
			drawingContext.Pop();
		}
	}
}
