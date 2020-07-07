using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace LedBoard.Controls
{
	public class TimelineTransitionDropAdorner : Adorner
	{
		private readonly Pen _Pen;

		public TimelineTransitionDropAdorner(TimelineControl owner, Color adornerColor) : base(owner)
		{
			_Pen = new Pen(new SolidColorBrush(adornerColor), 3);
			IsHitTestVisible = false;
		}

		public static readonly DependencyProperty AttachedItemProperty = DependencyProperty.Register(nameof(AttachedItem), typeof(TimelineItem), typeof(TimelineTransitionDropAdorner), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

		public TimelineItem AttachedItem
		{
			get => (TimelineItem)GetValue(AttachedItemProperty);
			set => SetValue(AttachedItemProperty, value);
		}

		protected override void OnRender(DrawingContext drawingContext)
		{
			var owner = (TimelineControl)AdornedElement;
			double height = AttachedItem.ActualHeight;
			double width = 1000 * owner.Zoom;
			double xOffset = Canvas.GetLeft(AttachedItem) + AttachedItem.ActualWidth - (width / 2);

			drawingContext.PushTransform(new TranslateTransform(xOffset, 0));
			drawingContext.DrawRectangle(null, _Pen, new Rect(0, 0, width, height));
			drawingContext.Pop();
		}
	}
}
