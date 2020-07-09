using LedBoard.Models;
using LedBoard.Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace LedBoard.Controls
{
	public class TimelineControl : Selector
	{
		private const string PART_Canvas = "PART_Canvas";
		private const string PART_ItemsPresenter = "PART_ItemsPresenter";

		private readonly TimelinePlaybackAdorner _PlaybackAdorner;

		private Canvas _Canvas;
		private TimelineDropAdorner _DropAdorner;
		private TimelineTransitionDropAdorner _TransitionDropAdorner;

		private TimelineItemMoveAdorner _MoveAdorner;
		private Point? _InitialItemOffset;
		private bool _IsMoving;

		public TimelineControl()
		{
			FrameworkElementFactory canvasFactory = new FrameworkElementFactory(typeof(Canvas));
			canvasFactory.SetValue(Panel.IsItemsHostProperty, true);
			canvasFactory.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Left);
			canvasFactory.SetValue(VerticalAlignmentProperty, VerticalAlignment.Stretch);
			canvasFactory.Name = PART_Canvas;
			ItemsPanel = new ItemsPanelTemplate
			{
				VisualTree = canvasFactory,
			};

			FrameworkElementFactory itemsPresenterFactory = new FrameworkElementFactory(typeof(ItemsPresenter))
			{
				Name = PART_ItemsPresenter
			};
			Template = new ControlTemplate
			{
				VisualTree = itemsPresenterFactory,
			};

			ClipToBounds = false;
			Focusable = true;
			AllowDrop = true;

			_PlaybackAdorner = new TimelinePlaybackAdorner(this);
		}

		#region Dependency properties

		public DataTemplate TransitionTemplate { get; set; }

		#region AdornerColor

		public static readonly DependencyProperty AdornerColorProperty = DependencyProperty.Register(nameof(AdornerColor), typeof(Color), typeof(TimelineControl), new PropertyMetadata(Colors.Black));

		public Color AdornerColor
		{
			get => (Color)GetValue(AdornerColorProperty);
			set => SetValue(AdornerColorProperty, value);
		}

		#endregion

		#region ScrubberColor

		public Color ScrubberColor
		{
			set
			{
				_PlaybackAdorner.Color = value;
			}
		}

		#endregion

		#region ScrubberPosition

		public static readonly DependencyProperty ScrubberPositionProperty = DependencyProperty.Register(nameof(ScrubberPosition), typeof(double), typeof(TimelineControl), new PropertyMetadata(0.0, OnScrubberPositionChanged));

		public double ScrubberPosition
		{
			get => (double)GetValue(ScrubberPositionProperty);
			set => SetValue(ScrubberPositionProperty, value);
		}

		private static void OnScrubberPositionChanged(DependencyObject owner, DependencyPropertyChangedEventArgs e)
		{
			var control = (TimelineControl)owner;
			control._PlaybackAdorner.OffsetX = control.ScrubberPosition * control.Zoom;
		}

		#endregion

		#region Zoom

		public static readonly DependencyProperty ZoomProperty = DependencyProperty.Register(nameof(Zoom), typeof(double), typeof(TimelineControl), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange, OnZoomChanged));

		public double Zoom
		{
			get => (double)GetValue(ZoomProperty);
			set => SetValue(ZoomProperty, value);
		}

		private static void OnZoomChanged(DependencyObject owner, DependencyPropertyChangedEventArgs e)
		{
			var control = (TimelineControl)owner;
			control.UpdateItemLayout();
			control._PlaybackAdorner.OffsetX = control.ScrubberPosition * control.Zoom;
		}

		#endregion

		#region TotalLength

		public static readonly DependencyProperty TotalLengthProperty = DependencyProperty.Register(nameof(TotalLength), typeof(double), typeof(TimelineControl), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure, OnTotalLengthChanged));

		public double TotalLength
		{
			get => (double)GetValue(TotalLengthProperty);
			set => SetValue(TotalLengthProperty, value);
		}

		private static void OnTotalLengthChanged(DependencyObject owner, DependencyPropertyChangedEventArgs e)
		{
			var control = (TimelineControl)owner;
			control.UpdateCanvasWidth();
		}

		#endregion

		#endregion

		public event EventHandler TransitionSelected;

		#region Control overrides

		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();
			var presenter = (ItemsPresenter)Template.FindName(PART_ItemsPresenter, this);
			presenter.ApplyTemplate();
			_Canvas = (Canvas)ItemsPanel.FindName(PART_Canvas, presenter);
			UpdateItemLayout();
			AdornerLayer.GetAdornerLayer(this).Add(_PlaybackAdorner);
		}

		protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
		{
			return new PointHitTestResult(this, hitTestParameters.HitPoint);
		}

		protected override void OnItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue)
		{
			base.OnItemsSourceChanged(oldValue, newValue);
			UpdateItemLayout();
		}

		protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
		{
			if (element is TimelineItem tli && item is SequenceEntry entry)
			{
				tli.Entry = entry;
			}
			base.PrepareContainerForItemOverride(element, item);
		}

		protected override void ClearContainerForItemOverride(DependencyObject element, object item)
		{
			if (element is TimelineItem tli)
			{
				tli.Entry = null;
			}
			base.ClearContainerForItemOverride(element, item);
		}

		protected override DependencyObject GetContainerForItemOverride()
		{
			var control = new TimelineItem(this);
			control.MouseDown += OnItemMouseDown;
			control.TransitionSelected += OnItemTransitionSelected;
			return control;
		}

		protected override bool IsItemItsOwnContainerOverride(object item)
		{
			return false;
		}

		protected override void OnSelectionChanged(SelectionChangedEventArgs e)
		{
			base.OnSelectionChanged(e);
			
			foreach (var item in e.RemovedItems)
			{
				TimelineItem tli = (TimelineItem)ItemContainerGenerator.ContainerFromItem(item);
				if (tli != null)
				{
					tli.IsSelected = false;
					AdornerLayer.GetAdornerLayer(this).Remove(tli.ResizeAdorner);
				}
			}
			foreach (var item in e.AddedItems)
			{
				TimelineItem tli = (TimelineItem)ItemContainerGenerator.ContainerFromItem(item);
				if (tli != null)
				{
					tli.IsSelected = true;
					AddAdorner(tli.ResizeAdorner);
				}
			}
		}

		protected override void OnDragEnter(DragEventArgs e)
		{
			e.Effects = DragDropEffects.None;
			SetAdornerHitTestVisible(false);
			if (e.Data.GetDataPresent(DataFormats.Serializable))
			{
				object dataItem = e.Data.GetData(DataFormats.Serializable);
				if (dataItem is StepDescriptor)
				{
					Point p = e.GetPosition(this);
					_DropAdorner = new TimelineDropAdorner(this, AdornerColor);
					(_DropAdorner.LeftOffset, _) = FindDropPosition(p.X);
					AdornerLayer.GetAdornerLayer(this).Add(_DropAdorner);
					e.Effects = DragDropEffects.Copy;
				}
				else if (dataItem is TransitionDescriptor)
				{
					Point p = e.GetPosition(this);
					(_, int index) = FindDropPosition(p.X);
					index--; // The transition location refers to the item immediately preceding it
					if (index > -1 && index < Items.Count)
					{
						var item = (TimelineItem)ItemContainerGenerator.ContainerFromIndex(index);
						if (item != null)
						{
							_TransitionDropAdorner = new TimelineTransitionDropAdorner(this, AdornerColor)
							{
								AttachedItem = item
							};
							AdornerLayer.GetAdornerLayer(this).Add(_TransitionDropAdorner);
							e.Effects = DragDropEffects.Copy;
						}
					}
				}
			}
			e.Handled = true;
		}

		protected override void OnDragLeave(DragEventArgs e)
		{
			if (_DropAdorner != null)
			{
				AdornerLayer.GetAdornerLayer(this).Remove(_DropAdorner);
				_DropAdorner = null;
			}
			if (_TransitionDropAdorner != null)
			{
				AdornerLayer.GetAdornerLayer(this).Remove(_TransitionDropAdorner);
				_TransitionDropAdorner = null;
			}
			SetAdornerHitTestVisible(true);
			e.Handled = true;
		}

		protected override void OnDragOver(DragEventArgs e)
		{
			SetAdornerHitTestVisible(false);
			e.Effects = DragDropEffects.None;
			if (e.Data.GetDataPresent(DataFormats.Serializable))
			{
				object dataItem = e.Data.GetData(DataFormats.Serializable);
				if (dataItem is StepDescriptor)
				{
					e.Effects = DragDropEffects.Copy;
					if (_DropAdorner != null)
					{
						var p = e.GetPosition(this);
						(_DropAdorner.LeftOffset, _) = FindDropPosition(p.X);
					}
				}
				else if (dataItem is TransitionDescriptor)
				{
					var p = e.GetPosition(this);
					(_, int index) = FindDropPosition(p.X);
					index--; // The transition location refers to the item immediately preceding it
					if (index > -1 && index < Items.Count)
					{
						var item = (TimelineItem)ItemContainerGenerator.ContainerFromIndex(index);
						if (item != null && _TransitionDropAdorner != null)
						{
							_TransitionDropAdorner.AttachedItem = item;
							e.Effects = DragDropEffects.Copy;
						}
					}
				}
			}
			e.Handled = true;
		}

		protected override void OnDrop(DragEventArgs e)
		{
			OnDragLeave(e);
			if (e.Data.GetDataPresent(DataFormats.Serializable))
			{
				object dataItem = e.Data.GetData(DataFormats.Serializable);
				if (dataItem is StepDescriptor descriptor)
				{
					if (ItemsSource is IList<SequenceEntry> collection)
					{
						var step = StepService.CreateStep(descriptor);
						var entry = new SequenceEntry(step);
						var p = e.GetPosition(this);
						(_, int index) = FindDropPosition(p.X);
						collection.Insert(index, entry);
					}
				}
				else if (dataItem is TransitionDescriptor transitionDescriptor)
				{
					Point p = e.GetPosition(this);
					(_, int index) = FindDropPosition(p.X);
					index--; // The transition location refers to the item immediately preceding it
					if (index > -1 && index < Items.Count)
					{
						var item = (TimelineItem)ItemContainerGenerator.ContainerFromIndex(index);
						if (item != null)
						{
							item.Entry.Transition = StepService.CreateTransition(transitionDescriptor);
						}
					}
				}
			}
			e.Handled = true;
		}

		protected override void OnMouseDown(MouseButtonEventArgs e)
		{
			base.OnMouseDown(e);

			// Mouse down on an item would have been handled by another handler, if we get here, the user is clicking somewhere there is not an item, so clear the selection
			if (!e.Handled)
			{
				SelectedItem = null;
				SelectTransition(null);
				Focus();
			}
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			bool isInDrag = e.LeftButton == MouseButtonState.Pressed && SelectedItem != null && _InitialItemOffset.HasValue;
			if (isInDrag && ItemContainerGenerator.ContainerFromItem(SelectedItem) is TimelineItem tli)
			{
				Point p = e.GetPosition(tli);
				Point pAbs = e.GetPosition(this);
				double deltaX = p.X - _InitialItemOffset.Value.X;
				if (Math.Abs(deltaX) > 10 || _IsMoving)
				{
					_IsMoving = true;

					// Display move adorner
					if (_MoveAdorner == null)
					{
						_MoveAdorner = new TimelineItemMoveAdorner(tli, AdornerColor);
						AddAdorner(_MoveAdorner);
					}
					double left = Canvas.GetLeft(tli);
					if (deltaX < -left) deltaX = -left;
					if (deltaX > ActualWidth - tli.ActualWidth - left) deltaX = ActualWidth - tli.ActualWidth - left;
					_MoveAdorner.OffsetX = deltaX;

					// Display drop adorner for drop target
					if (_DropAdorner == null)
					{
						_DropAdorner = new TimelineDropAdorner(this, AdornerColor);
						AddAdorner(_DropAdorner);
					}
					(_DropAdorner.LeftOffset, _) = FindDropPosition(pAbs.X);
				}
				e.Handled = true;
			}
			base.OnMouseMove(e);
		}

		protected override void OnMouseUp(MouseButtonEventArgs e)
		{
			if (_IsMoving)
			{
				_InitialItemOffset = null;
				_IsMoving = false;
				if (_MoveAdorner != null)
				{
					AdornerLayer.GetAdornerLayer(this).Remove(_MoveAdorner);
					_MoveAdorner = null;
				}
				if (_DropAdorner != null)
				{
					AdornerLayer.GetAdornerLayer(this).Remove(_DropAdorner);
					_DropAdorner = null;
				}

				if (ItemsSource is ObservableCollection<SequenceEntry> collection)
				{
					var p = e.GetPosition(this);
					(_, int index) = FindDropPosition(p.X);
					if (index > SelectedIndex) index--;
					collection.Move(SelectedIndex, index);
				}

				e.Handled = true;
			}
			base.OnMouseUp(e);
		}

		protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
		{
			base.OnRenderSizeChanged(sizeInfo);
			_PlaybackAdorner.Height = ActualHeight;
		}

		#endregion

		private void OnItemMouseDown(object sender, MouseButtonEventArgs e)
		{
			// If this MouseDown event was actually on an item, select that item
			if (sender is TimelineItem item)
			{
				SelectTransition(null);
				SelectedIndex = ItemContainerGenerator.IndexFromContainer(item);
				_InitialItemOffset = e.GetPosition(item);
				_IsMoving = false;
				Focus();
				e.Handled = true;
			}
		}

		private void OnItemTransitionSelected(object sender, EventArgs e)
		{
			if (sender is TimelineItem item)
			{
				SelectedItem = null;
				SelectTransition(item);
				Focus();
			}
		}

		private (double, int) FindDropPosition(double offsetLeft)
		{
			for (int i = Items.Count - 1; i >= 0; i--)
			{
				TimelineItem tli = (TimelineItem)ItemContainerGenerator.ContainerFromIndex(i);
				if (tli != null)
				{
					double itemLeft = Canvas.GetLeft(tli);
					if (itemLeft < offsetLeft)
					{
						double halfOffset = itemLeft + tli.ActualWidth / 2.0;
						bool isAfterHalf = offsetLeft > halfOffset;
						return (isAfterHalf ? itemLeft + tli.ActualWidth : itemLeft, isAfterHalf ? i + 1 : i);
					}
				}
			}

			return (0, 0);
		}

		private void UpdateCanvasWidth()
		{
			double lastEntryTransitionExtra = 0;
			if (Items.Count > 0)
			{
				var lastItem = (TimelineItem)ItemContainerGenerator.ContainerFromIndex(Items.Count - 1);
				if (lastItem != null) lastEntryTransitionExtra = lastItem.TransitionAdorner.ActualWidth / 2;
			}
			if (_Canvas != null) _Canvas.Width = TotalLength * Zoom + lastEntryTransitionExtra;
		}

		public void UpdateItemLayout()
		{
			foreach (var item in Items)
			{
				var uiItem = (TimelineItem)ItemContainerGenerator.ContainerFromItem(item);
				if (uiItem != null)
				{
					uiItem.UpdateBounds();
				}
			}
			UpdateCanvasWidth();
		}

		private void SelectTransition(TimelineItem item)
		{
			int? selectedIndex = item != null ? (int?)ItemContainerGenerator.IndexFromContainer(item) : null;
			for (int i = 0; i < Items.Count; i++)
			{
				var container = (TimelineItem)ItemContainerGenerator.ContainerFromIndex(i);
				container.TransitionAdorner.IsSelected = i == selectedIndex;
			}
			TransitionSelected?.Invoke(item?.Entry?.Transition, EventArgs.Empty);
		}

		private void SetAdornerHitTestVisible(bool value)
		{
			AdornerLayer.GetAdornerLayer(this).IsHitTestVisible = value;
		}

		public void AddAdorner(Adorner adorner)
		{
			var layer = AdornerLayer.GetAdornerLayer(this);
			layer.Remove(_PlaybackAdorner);
			layer.Add(adorner);
			layer.Add(_PlaybackAdorner);
		}
	}
}
