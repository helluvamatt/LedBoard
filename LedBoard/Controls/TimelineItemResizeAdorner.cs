using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace LedBoard.Controls
{
	public class TimelineItemResizeAdorner : Adorner
	{
		private readonly Brush _HandleBrush;
		private readonly Brush _ArrowBrush;

		private readonly Geometry _RightArrowGeom;

		private Point? _InitialPoint;

		public TimelineItemResizeAdorner(TimelineItem adornedElement) : base(adornedElement)
		{
			Cursor = Cursors.SizeWE;
			_HandleBrush = new SolidColorBrush(Color.FromArgb(0xAA, 0xAA, 0xAA, 0xAA));
			_ArrowBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0x77, 0x77, 0x77));
			_RightArrowGeom = Geometry.Parse("M 0 0 L 8 4 L 0 8 z");
		}

		protected override void OnMouseDown(MouseButtonEventArgs e)
		{
			if (e.LeftButton == MouseButtonState.Pressed)
			{
				_InitialPoint = e.GetPosition(this);
				CaptureMouse();
				e.Handled = true;
			}
		}

		protected override void OnMouseUp(MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left || _InitialPoint.HasValue)
			{
				_InitialPoint = null;
				ReleaseMouseCapture();
				e.Handled = true;
			}
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			if (e.LeftButton == MouseButtonState.Pressed && _InitialPoint.HasValue)
			{
				Point p = e.GetPosition(this);
				double delta = p.X - _InitialPoint.Value.X;
				((TimelineItem)AdornedElement).HandleResize(delta);
				_InitialPoint = p;
			}
		}

		protected override void OnRender(DrawingContext drawingContext)
		{
			var adornedItem = (TimelineItem)AdornedElement;
			var bounds = new Rect(0, 0, adornedItem.ActualWidth, adornedItem.ActualHeight);

			// Draw right resize handle
			var rightBounds = new Rect(bounds.Right - 16, 0, 16, bounds.Height);
			drawingContext.DrawRectangle(_HandleBrush, null, rightBounds);
			drawingContext.PushTransform(new TranslateTransform(rightBounds.Left + 4, rightBounds.Top + (rightBounds.Height - 8) / 2));
			drawingContext.DrawGeometry(_ArrowBrush, null, _RightArrowGeom);
			drawingContext.Pop();
		}
	}
}
