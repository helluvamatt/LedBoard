using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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

		#region IsTransition

		public static readonly DependencyProperty IsTransitionProperty = DependencyProperty.Register(nameof(IsTransition), typeof(bool), typeof(TimelineDropAdorner), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

		public bool IsTransition
		{
			get => (bool)GetValue(IsTransitionProperty);
			set => SetValue(IsTransitionProperty, value);
		}

		#endregion

		#endregion

		protected override void OnRender(DrawingContext drawingContext)
		{
			double height = (AdornedElement as FrameworkElement)?.ActualHeight ?? AdornedElement.DesiredSize.Height;
			if (IsTransition)
			{
				//var geom = Geometry.Parse($"M 0 0 L 8 0 L 5 8 L 5 {height} L 3 {height} L 3 8 z");
				//drawingContext.PushTransform(new TranslateTransform(LeftOffset - 4, 0));
				//drawingContext.DrawGeometry(Brushes.Black, null, geom);
				//drawingContext.Pop();
			}
			else
			{
				var geom = Geometry.Parse($"M 0 0 L 16 0 L 10 16 L 10 {height} L 6 {height} L 6 16 z");
				drawingContext.PushTransform(new TranslateTransform(LeftOffset - 8, 0));
				drawingContext.DrawGeometry(_Brush, null, geom);
				drawingContext.Pop();
			}
		}
	}
}
