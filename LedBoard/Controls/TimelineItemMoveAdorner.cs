using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace LedBoard.Controls
{
	public class TimelineItemMoveAdorner : Adorner
	{
		private readonly Brush _Brush;
		private readonly Pen _Pen;
		private readonly Rect _Bounds;
		private readonly TranslateTransform _Transform;

		public TimelineItemMoveAdorner(TimelineItem adornedElement, Color adornerColor) : base(adornedElement)
		{
			_Bounds = new Rect(new Size(adornedElement.ActualWidth, adornedElement.ActualHeight));
			_Transform = new TranslateTransform();
			_Brush = new SolidColorBrush(Color.FromArgb(0x99, adornerColor.R, adornerColor.G, adornerColor.B));
			_Pen = new Pen(new SolidColorBrush(adornerColor), 2.0) { DashStyle = new DashStyle(new double[] { 10, 10 }, 0) };
			IsHitTestVisible = false;
		}

		public static readonly DependencyProperty OffsetXProperty = DependencyProperty.Register(nameof(OffsetX), typeof(double), typeof(TimelineItemMoveAdorner), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

		public double OffsetX
		{
			get => (double)GetValue(OffsetXProperty);
			set => SetValue(OffsetXProperty, value);
		}

		protected override void OnRender(DrawingContext drawingContext)
		{
			_Transform.X = OffsetX;
			drawingContext.PushTransform(_Transform);
			drawingContext.DrawRectangle(_Brush, _Pen, _Bounds);
			drawingContext.Pop();
		}
	}
}
