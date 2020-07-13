using LedBoard.Converters;
using LedBoard.Models;
using LedBoard.Properties;
using LedBoard.ViewModels;
using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LedBoard.Views
{
	/// <summary>
	/// Interaction logic for ProjectPage.xaml
	/// </summary>
	public partial class ProjectPage : Page
	{
		private readonly double[] _ZoomValues;

		public ProjectPage()
		{
			InitializeComponent();
			_ZoomValues = ((DoubleDescriptor[])Resources["BoardZoomOptions"]).Select(dd => dd.Value).ToArray();
			DataContextChanged += OnDataContextChanged;
		}

		private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			if (e.OldValue is ShellViewModel oldVm) oldVm.SequencerPropertyChanged -= OnSequencePropertyChanged;
			if (e.NewValue is ShellViewModel newVm) newVm.SequencerPropertyChanged += OnSequencePropertyChanged;
		}

		private void OnSequencePropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(Sequence.CurrentTime))
			{
				Dispatcher.Invoke(() =>
				{
					var dc = (ShellViewModel)DataContext;
					// Compute the scroll offset
					var widthConverter = (TimelineWidthConverter)Resources["timelineWidthConverter"];
					double positionOfCaret = (double)widthConverter.Convert(new object[] { dc.Sequencer.Sequence.CurrentTime, Settings.Default.TimelineZoom, }, typeof(double), null, null);
					double leftOffsetThreshold = positionOfCaret - timelineScroller.ViewportWidth * 0.9;
					double rightOffsetThreshold = positionOfCaret - timelineScroller.ViewportWidth * 0.1;
					if (timelineScroller.HorizontalOffset < leftOffsetThreshold) timelineScroller.ScrollToHorizontalOffset(leftOffsetThreshold);
					else if (timelineScroller.HorizontalOffset > rightOffsetThreshold) timelineScroller.ScrollToHorizontalOffset(rightOffsetThreshold);
				});
			}
			else if (e.PropertyName == nameof(Sequence.IsDirty))
			{
				Dispatcher.Invoke(() =>
				{
					var dc = (ShellViewModel)DataContext;
					var window = this.TryFindParent<Window>();
					if (dc.IsDirty)
					{
						Interop.User32.ShutdownBlockReasonCreate(new WindowInteropHelper(window).Handle, "You have unsaved changes to your project.");
					}
					else
					{
						Interop.User32.ShutdownBlockReasonDestroy(new WindowInteropHelper(window).Handle);
					}
				});
			}

			CommandManager.InvalidateRequerySuggested();
		}

		private void OnBoardContainerMouseWheel(object sender, MouseWheelEventArgs e)
		{
			int index = Array.IndexOf(_ZoomValues, Settings.Default.BoardZoom);
			if (e.Delta > 0)
			{
				index++;
				if (index >= _ZoomValues.Length) index = _ZoomValues.Length - 1;
			}
			else
			{
				index--;
				if (index < 0) index = 0;
			}
			Settings.Default.BoardZoom = _ZoomValues[index];
			e.Handled = true;
		}

		private void OnToolboxMouseMove(object sender, MouseEventArgs e)
		{
			if (sender is ListBox source && e.LeftButton == MouseButtonState.Pressed && source.SelectedItem != null)
			{
				DragDrop.DoDragDrop(source, new DataObject(DataFormats.Serializable, source.SelectedItem), DragDropEffects.Copy);
			}
		}

		private void OnTimelineControlRequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
		{
			// Don't bother trying to scroll the timeline automatically
			e.Handled = true;
		}
	}
}
