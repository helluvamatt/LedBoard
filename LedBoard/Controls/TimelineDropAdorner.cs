using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace LedBoard.Controls
{
	public class TimelineDropAdorner : Adorner
	{
		private readonly SolidColorBrush _Brush;

		public TimelineDropAdorner(UIElement adornedElement, Color brushColor) : base(adornedElement)
		{
			_Brush = new SolidColorBrush(brushColor);
			IsHitTestVisible = false;
		}

		#region Dependency properties

		#region LeftOffset

		public static readonly DependencyProperty LeftOffsetProperty = DependencyProperty.Register(nameof(LeftOffset), typeof(double), typeof(TimelineDropAdorner), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

		public double LeftOffset
		{
			get => (double)GetValue(LeftOffsetProperty);
			set => SetValue(LeftOffsetProperty, value);
		}

		#endregion

		#endregion

		protected override void OnRender(DrawingContext drawingContext)
		{
			var timelineControl = (TimelineControl)AdornedElement;
			double height = timelineControl.ActualHeight;
			var geom = Geometry.Parse($"M 0 0 L 16 0 L 10 16 L 10 {height} L 6 {height} L 6 16 z");
			drawingContext.PushTransform(new TranslateTransform(LeftOffset - 8, 0));
			drawingContext.DrawGeometry(_Brush, null, geom);
			drawingContext.Pop();
		}
	}
}
