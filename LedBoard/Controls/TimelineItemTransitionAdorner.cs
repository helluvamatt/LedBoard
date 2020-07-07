﻿using LedBoard.Services;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Media;

namespace LedBoard.Controls
{
	public class TimelineItemTransitionAdorner : Adorner
	{
		//private readonly Pen _BorderPen;
		//private readonly Brush _BackgroundBrush;
		private readonly VisualCollection _Visuals;
		private readonly ContentPresenter _ContentPresenter;
		private readonly TranslateTransform _Transform;

		public TimelineItemTransitionAdorner(TimelineItem item, TimelineControl owner, DataTemplate template) : base(item)
		{
			Owner = owner;
			_Transform = new TranslateTransform();
			_Visuals = new VisualCollection(this);
			_ContentPresenter = new ContentPresenter()
			{
				ContentTemplate = template,
				RenderTransform = _Transform,
				Content = this,
			};
			_Visuals.Add(_ContentPresenter);
		}

		public TimelineControl Owner { get; }

		public Point Offset
		{
			get => new Point(_Transform.X, _Transform.Y);
			set
			{
				_Transform.X = value.X;
				_Transform.Y = value.Y;
			}
		}

		public static readonly DependencyProperty IconProperty = DependencyProperty.Register(nameof(Icon), typeof(ImageSource), typeof(TimelineItemTransitionAdorner), new PropertyMetadata(null));

		public ImageSource Icon
		{
			get => (ImageSource)GetValue(IconProperty);
			set => SetValue(IconProperty, value);
		}

		public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.Register(nameof(IsSelected), typeof(bool), typeof(TimelineItemTransitionAdorner), new PropertyMetadata(false));

		public bool IsSelected
		{
			get => (bool)GetValue(IsSelectedProperty);
			set => SetValue(IsSelectedProperty, value);
		}

		protected override Size MeasureOverride(Size constraint)
		{
			_ContentPresenter.Measure(constraint);
			return _ContentPresenter.DesiredSize;
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			_ContentPresenter.Arrange(new Rect(0, 0, finalSize.Width, finalSize.Height));
			return _ContentPresenter.RenderSize;
		}

		public override void OnApplyTemplate()
		{
			_ContentPresenter.ApplyTemplate();
		}

		protected override Visual GetVisualChild(int index) => _Visuals[index];

		protected override int VisualChildrenCount => _Visuals.Count;

		//protected override void OnRender(DrawingContext drawingContext)
		//{
		//	var item = (TimelineItem)AdornedElement;
		//	if (item.Entry.Transition != null)
		//	{
		//		ImageSource icon = StepService.GetIconForTransition(item.Entry.Transition);
		//		double width = item.Entry.Transition.Length.TotalMilliseconds * _Owner.Zoom;
		//		double height = ICON_SIZE + PADDING * 2;
		//		double offsetX = item.ActualWidth - width / 2;
		//		drawingContext.PushTransform(new TranslateTransform(offsetX, item.ActualHeight));
		//		drawingContext.DrawRectangle(_BackgroundBrush, _BorderPen, new Rect(0, 0, width, height));
		//		if (icon != null)
		//		{
		//			var iconBounds = new Rect((width - ICON_SIZE) / 2, PADDING, ICON_SIZE, ICON_SIZE);
		//			drawingContext.DrawImage(icon, iconBounds);
		//		}
		//		drawingContext.Pop();
		//	}
		//}
	}
}
