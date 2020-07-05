using LedBoard.Models;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace LedBoard.Controls
{
	public class TimelineItem : ContentControl
	{
		private readonly TimelineControl _Owner;

		private Border _Border;
		private SequenceEntry _Entry;

		public TimelineItem(TimelineControl owner)
		{
			_Owner = owner;
			ResizeAdorner = new TimelineItemResizeAdorner(this, _Owner.AdornerColor);
		}

		public TimelineItemResizeAdorner ResizeAdorner { get; }

		public SequenceEntry Entry
		{
			get => _Entry;
			set
			{
				if (_Entry != null) _Entry.PropertyChanged -= OnEntryPropertyChanged;
				_Entry = value;
				if (_Entry != null) _Entry.PropertyChanged += OnEntryPropertyChanged;
				UpdateBounds();
			}
		}

		public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.Register(
			nameof(IsSelected),
			typeof(bool),
			typeof(TimelineItem),
			new FrameworkPropertyMetadata(
				false,
				FrameworkPropertyMetadataOptions.Journal | FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.AffectsParentMeasure));

		public bool IsSelected
		{
			get => (bool)GetValue(IsSelectedProperty);
			set => SetValue(IsSelectedProperty, value);
		}

		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();
			_Border = (Border)Template.FindName("PART_Border", this);
			UpdateBounds();
		}

		private void OnEntryPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			_Owner.UpdateItemLayout();
		}

		public void UpdateBounds()
		{
			if (_Entry != null)
			{
				Canvas.SetLeft(this, _Entry.StartTime.TotalMilliseconds * _Owner.Zoom);
				if (_Border != null)
				{
					_Border.Width = _Entry.Length.TotalMilliseconds * _Owner.Zoom;
				}
			}
		}

		public bool HandleResize(double delta)
		{
			if (_Entry != null)
			{
				// Modify delta by the current zoom
				delta /= _Owner.Zoom;

				// Modify the entry
				return _Entry.HandleResize(delta);
			}
			return true;
		}
	}
}
