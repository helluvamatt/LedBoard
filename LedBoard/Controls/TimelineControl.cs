using LedBoard.Models;
using LedBoard.Services;
using System;
using System.Collections;
using System.Collections.Generic;
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

		private Canvas _Canvas;
		private TimelineDropAdorner _DropAdorner;

		public TimelineControl()
		{
			FrameworkElementFactory canvasFactory = new FrameworkElementFactory(typeof(Canvas));
			canvasFactory.SetValue(Panel.IsItemsHostProperty, true);
			canvasFactory.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Left);
			canvasFactory.Name = PART_Canvas;
			ItemsPanel = new ItemsPanelTemplate
			{
				VisualTree = canvasFactory,
			};

			FrameworkElementFactory itemsPresenterFactory = new FrameworkElementFactory(typeof(ItemsPresenter));
			itemsPresenterFactory.Name = PART_ItemsPresenter;
			Template = new ControlTemplate
			{
				VisualTree = itemsPresenterFactory,
			};

			ClipToBounds = false;
			Focusable = true;
			AllowDrop = true;
		}

		#region Dependency properties

		#region AdornerColor

		public static readonly DependencyProperty AdornerColorProperty = DependencyProperty.Register(nameof(AdornerColor), typeof(Color), typeof(TimelineControl), new PropertyMetadata(Colors.Black));

		public Color AdornerColor
		{
			get => (Color)GetValue(AdornerColorProperty);
			set => SetValue(AdornerColorProperty, value);
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

		#region Control overrides

		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();
			var presenter = (ItemsPresenter)Template.FindName(PART_ItemsPresenter, this);
			presenter.ApplyTemplate();
			_Canvas = (Canvas)ItemsPanel.FindName(PART_Canvas, presenter);
			UpdateItemLayout();
		}

		protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
		{
			return new PointHitTestResult(this, hitTestParameters.HitPoint);
		}

		protected override void OnItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue)
		{
			base.OnItemsSourceChanged(oldValue, newValue);
			AllowDrop = newValue is ICollection;
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
			control.MouseUp += OnItemMouseUp;
			return control;
		}

		protected override bool IsItemItsOwnContainerOverride(object item)
		{
			return false;
		}

		protected override void OnSelectionChanged(SelectionChangedEventArgs e)
		{
			base.OnSelectionChanged(e);
			var adornerLayer = AdornerLayer.GetAdornerLayer(this);
			foreach (var item in e.RemovedItems)
			{
				TimelineItem tli = (TimelineItem)ItemContainerGenerator.ContainerFromItem(item);
				if (tli != null)
				{
					tli.IsSelected = false;
					adornerLayer.Remove(tli.ResizeAdorner);
				}
			}
			foreach (var item in e.AddedItems)
			{
				TimelineItem tli = (TimelineItem)ItemContainerGenerator.ContainerFromItem(item);
				if (tli != null)
				{
					tli.IsSelected = true;
					adornerLayer.Add(tli.ResizeAdorner);
				}
			}
		}

		protected override void OnDragEnter(DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.Serializable))
			{
				if (e.Data.GetData(DataFormats.Serializable) is StepDescriptor)
				{
					Point p = e.GetPosition(this);
					_DropAdorner = new TimelineDropAdorner(this, AdornerColor);
					(_DropAdorner.LeftOffset, _) = FindDropPosition(p.X);
					AdornerLayer layer = AdornerLayer.GetAdornerLayer(this);
					layer.Add(_DropAdorner);
				}
			}
			e.Handled = true;
		}

		protected override void OnDragLeave(DragEventArgs e)
		{
			if (_DropAdorner != null)
			{
				AdornerLayer layer = AdornerLayer.GetAdornerLayer(this);
				layer.Remove(_DropAdorner);
				_DropAdorner = null;
			}
			e.Handled = true;
		}

		protected override void OnDragOver(DragEventArgs e)
		{
			e.Effects = DragDropEffects.None;
			if (e.Data.GetDataPresent(DataFormats.Serializable))
			{
				if (e.Data.GetData(DataFormats.Serializable) is StepDescriptor)
				{
					e.Effects = DragDropEffects.Copy;
					if (_DropAdorner != null)
					{
						var p = e.GetPosition(this);
						(_DropAdorner.LeftOffset, _) = FindDropPosition(p.X);
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
				if (e.Data.GetData(DataFormats.Serializable) is StepDescriptor descriptor)
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
			}
			e.Handled = true;
		}

		protected override void OnMouseUp(MouseButtonEventArgs e)
		{
			base.OnMouseUp(e);
			if (!e.Handled)
			{
				SelectedItem = null;
				Focus();
			}
		}

		#endregion

		private void OnItemMouseUp(object sender, MouseButtonEventArgs e)
		{
			if (sender is TimelineItem item)
			{
				SelectedIndex = ItemContainerGenerator.IndexFromContainer(item);
				Focus();
				e.Handled = true;
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
			if (_Canvas != null) _Canvas.Width = TotalLength * Zoom;
		}

		public void UpdateItemLayout()
		{
			UpdateCanvasWidth();

			foreach (var item in Items)
			{
				var uiItem = (TimelineItem)ItemContainerGenerator.ContainerFromItem(item);
				uiItem.UpdateBounds();
			}
		}
	}
}
