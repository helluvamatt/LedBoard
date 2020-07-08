using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace LedBoard.Controls
{
	public class TimelineItemTransitionAdorner : Adorner
	{
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
	}
}
