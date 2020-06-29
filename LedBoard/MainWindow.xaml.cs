using LedBoard.Converters;
using LedBoard.Models;
using LedBoard.Models.Steps;
using LedBoard.Services;
using LedBoard.ViewModels;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
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
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LedBoard
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : MetroWindow, IDialogService
	{
		public MainWindow()
		{
			ZoomInCommand = new DelegateCommand(OnZoomIn, () => Zoom < MaxZoom);
			ZoomOutCommand = new DelegateCommand(OnZoomOut, () => Zoom > MinZoom);
			ExportCommand = new DelegateCommand(OnExport, () => Sequencer != null);
			InitializeComponent();
		}

		public ICommand ZoomInCommand { get; }
		public ICommand ZoomOutCommand { get; }
		public ICommand ExportCommand { get; }

		#region Dependency properties

		#region Sequencer

		public static readonly DependencyProperty SequencerProperty = DependencyProperty.Register(nameof(Sequencer), typeof(SequencerViewModel), typeof(MainWindow), new PropertyMetadata(null, OnSequencerChange));

		public SequencerViewModel Sequencer
		{
			get => (SequencerViewModel)GetValue(SequencerProperty);
			set => SetValue(SequencerProperty, value);
		}

		private static void OnSequencerChange(DependencyObject owner, DependencyPropertyChangedEventArgs e)
		{
			var window = (MainWindow)owner;
			if (e.OldValue is SequencerViewModel oldSequencer)
			{
				oldSequencer.SelectedItemChanged -= window.OnSelectedItemChanged;
				oldSequencer.Sequence.PropertyChanged -= window.OnSequencePropertyChanged;
			}
			if (e.NewValue is SequencerViewModel newSequencer)
			{
				newSequencer.Sequence.PropertyChanged += window.OnSequencePropertyChanged;
				newSequencer.SelectedItemChanged += window.OnSelectedItemChanged;
			}
		}

		#endregion

		#region NewBoardWidth

		public static readonly DependencyProperty NewBoardWidthProperty = DependencyProperty.Register(nameof(NewBoardWidth), typeof(int), typeof(MainWindow), new PropertyMetadata(64));

		public int NewBoardWidth
		{
			get => (int)GetValue(NewBoardWidthProperty);
			set => SetValue(NewBoardWidthProperty, value);
		}

		#endregion

		#region NewBoardHeight

		public static readonly DependencyProperty NewBoardHeightProperty = DependencyProperty.Register(nameof(NewBoardHeight), typeof(int), typeof(MainWindow), new PropertyMetadata(16));

		public int NewBoardHeight
		{
			get => (int)GetValue(NewBoardHeightProperty);
			set => SetValue(NewBoardHeightProperty, value);
		}

		#endregion

		#region IsProjectSettingsOpen

		public static readonly DependencyProperty IsProjectSettingsOpenProperty = DependencyProperty.Register(nameof(IsProjectSettingsOpen), typeof(bool), typeof(MainWindow), new PropertyMetadata(true));

		public bool IsProjectSettingsOpen
		{
			get => (bool)GetValue(IsProjectSettingsOpenProperty);
			set => SetValue(IsProjectSettingsOpenProperty, value);
		}

		#endregion

		#region Zoom

		public double MinZoom => 1;
		public double MaxZoom => 10;

		public static readonly DependencyProperty ZoomProperty = DependencyProperty.Register(nameof(Zoom), typeof(double), typeof(MainWindow), new PropertyMetadata(4.0));

		public double Zoom
		{
			get => (double)GetValue(ZoomProperty);
			set => SetValue(ZoomProperty, value);
		}

		#endregion

		#region ToolboxTabPage

		public static readonly DependencyProperty ToolboxTabPageProperty = DependencyProperty.Register(nameof(ToolboxTabPage), typeof(int), typeof(MainWindow), new PropertyMetadata(0));

		public int ToolboxTabPage
		{
			get => (int)GetValue(ToolboxTabPageProperty);
			set => SetValue(ToolboxTabPageProperty, value);
		}

		#endregion

		#region ConfigurationModel

		public static readonly DependencyProperty ConfigurationModelProperty = DependencyProperty.Register(nameof(ConfigurationModel), typeof(SequenceStepConfigViewModel), typeof(MainWindow), new PropertyMetadata(null));

		public SequenceStepConfigViewModel ConfigurationModel
		{
			get => (SequenceStepConfigViewModel)GetValue(ConfigurationModelProperty);
			set => SetValue(ConfigurationModelProperty, value);
		}

		#endregion

		#endregion

		#region Event handlers

		private void OnProjectSettingsClick(object sender, RoutedEventArgs e)
		{
			IsProjectSettingsOpen = true;
		}

		private void OnNewProjectClick(object sender, RoutedEventArgs e)
		{
			Sequencer = new SequencerViewModel(this, NewBoardWidth, NewBoardHeight);
			IsProjectSettingsOpen = false;
		}

		private void OnCancelClick(object sender, RoutedEventArgs e)
		{
			if (Sequencer == null) Close();
			else IsProjectSettingsOpen = false;
		}

		private void OnZoomOut()
		{
			Zoom = Math.Max(MinZoom, Zoom - 1);
		}

		private void OnZoomIn()
		{
			Zoom = Math.Min(MaxZoom, Zoom + 1);
		}

		private void OnSelectedItemChanged(object sender, EventArgs e)
		{
			if (Sequencer?.SelectedItem != null)
			{
				ConfigurationModel = new SequenceStepConfigViewModel(Sequencer.SelectedItem.Step, this);
				ToolboxTabPage = 1;
			}
			else
			{
				ConfigurationModel = null;
			}
		}

		private void OnSequencePropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(Sequence.CurrentStep))
			{
				Dispatcher.Invoke(() =>
				{
					// Compute the scroll offset
					var widthConverter = (TimelineWidthConverter)Resources["timelineWidthConverter"];
					double positionOfCaret = (double)widthConverter.Convert(new object[] { Sequencer.Sequence.CurrentStep, Zoom, }, typeof(double), null, null);
					double leftOffsetThreshold = positionOfCaret - timelineScroller.ViewportWidth * 0.9;
					double rightOffsetThreshold = positionOfCaret - timelineScroller.ViewportWidth * 0.1;
					if (timelineScroller.HorizontalOffset < leftOffsetThreshold) timelineScroller.ScrollToHorizontalOffset(leftOffsetThreshold);
					else if (timelineScroller.HorizontalOffset > rightOffsetThreshold) timelineScroller.ScrollToHorizontalOffset(rightOffsetThreshold);
				});
			}
		}

		private void OnExport()
		{

		}

		#endregion

		#region IDialogService imple

		public string OpenFileDialog(string title, string filters, string initialDirectory = null)
		{
			var ofd = new OpenFileDialog
			{
				CheckFileExists = true,
				Filter = filters,
				InitialDirectory = initialDirectory,
				Title = title,
			};
			return (ofd.ShowDialog(this) ?? false) ? ofd.FileName : null;
		}

		public async void ConfirmDialog(string title, string message, Action<bool> callback, string affirmativeBtnText = null, string negativeBtnText = null)
		{
			var settings = new MetroDialogSettings
			{
				AffirmativeButtonText = affirmativeBtnText ?? "Yes",
				NegativeButtonText = negativeBtnText ?? "No",
				AnimateShow = false,
				AnimateHide = false,
				DefaultButtonFocus = MessageDialogResult.Negative,
			};
			var result = await this.ShowMessageAsync(title, message, MessageDialogStyle.AffirmativeAndNegative, settings);
			callback.Invoke(result == MessageDialogResult.Affirmative);
		}

		#endregion
	}
}
