using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace LedBoard
{
	public class TimelineControl : Selector
	{
		public TimelineControl()
		{
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
			foreach (var item in e.RemovedItems)
			{
				TimelineItem tli = (TimelineItem)ItemContainerGenerator.ContainerFromItem(item);
				if (tli != null) tli.IsSelected = false;
			}
			foreach (var item in e.AddedItems)
			{
				TimelineItem tli = (TimelineItem)ItemContainerGenerator.ContainerFromItem(item);
				if (tli != null) tli.IsSelected = true;
			}
		}

		private void OnItemMouseUp(object sender, MouseButtonEventArgs e)
		{
			if (sender is TimelineItem item)
			{
				SelectedIndex = ItemContainerGenerator.IndexFromContainer(item);
			}
		}
	}

	public class TimelineItem : ContentControl
	{
		private readonly TimelineControl _Owner;

		public TimelineItem(TimelineControl owner)
		{
			_Owner = owner;
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
	}
}
