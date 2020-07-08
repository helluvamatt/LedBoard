using LedBoard.Models;
using LedBoard.Services;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;

namespace LedBoard.Controls
{
	public class TimelineItem : ContentControl
	{
		private readonly TimelineControl _Owner;

		private Border _Border;
		private SequenceEntry _Entry;
		private bool _HasTransitionAdorner;

		public TimelineItem(TimelineControl owner)
		{
			_Owner = owner;
			ResizeAdorner = new TimelineItemResizeAdorner(this, _Owner.AdornerColor);
			TransitionAdorner = new TimelineItemTransitionAdorner(this, _Owner, _Owner.TransitionTemplate);
			TransitionAdorner.MouseDown += OnTransitionAdornerMouseDown;
		}

		public TimelineItemResizeAdorner ResizeAdorner { get; }
		public TimelineItemTransitionAdorner TransitionAdorner { get; }

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

		public event EventHandler TransitionSelected;

		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();
			_Border = (Border)Template.FindName("PART_Border", this);
			UpdateBounds();
		}

		protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
		{
			base.OnRenderSizeChanged(sizeInfo);
			UpdateTransition();
		}

		private void OnEntryPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(SequenceEntry.Transition))
			{
				UpdateTransition();
			}
			else
			{
				_Owner.UpdateItemLayout();
			}
		}

		private void OnTransitionAdornerMouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.LeftButton == MouseButtonState.Pressed)
			{
				TransitionSelected?.Invoke(this, EventArgs.Empty);
				e.Handled = true;
			}
		}

		public void UpdateBounds()
		{
			if (_Entry != null)
			{
				Canvas.SetLeft(this, _Entry.StartTime.TotalMilliseconds * _Owner.Zoom);
				double width = _Entry.Length.TotalMilliseconds * _Owner.Zoom;
				if (_Border != null) _Border.Width = width;
				UpdateLayout();
				UpdateTransition();
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

		private void UpdateTransition()
		{
			if (_Entry.Transition != null)
			{
				if (!_HasTransitionAdorner)
				{
					_Owner.AddAdorner(TransitionAdorner);
					_HasTransitionAdorner = true;
				}
				double itemWidth = _Border?.ActualWidth ?? ActualWidth;
				double itemHeight = _Border?.ActualHeight ?? ActualHeight;
				double width = _Entry.Transition.Length.TotalMilliseconds * _Owner.Zoom;
				TransitionAdorner.Width = width;
				TransitionAdorner.Icon = StepService.GetIconForTransition(_Entry.Transition);
				TransitionAdorner.Offset = new Point(itemWidth - width / 2, itemHeight);
				TransitionAdorner.UpdateLayout();
			}
			else
			{
				AdornerLayer.GetAdornerLayer(_Owner).Remove(TransitionAdorner);
				_HasTransitionAdorner = false;
			}
		}
	}
}
