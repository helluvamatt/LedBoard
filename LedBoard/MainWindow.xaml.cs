using LedBoard.Controls;
using LedBoard.Converters;
using LedBoard.Models;
using LedBoard.Services;
using LedBoard.Services.Export;
using LedBoard.ViewModels;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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
			ExportCommand = new DelegateCommand(() => IsExportOpen = true, () => Sequencer != null && Sequencer.Sequence.Length > TimeSpan.Zero);
			ExportRenderCommand = new DelegateCommand(OnExportRender, () => Sequencer != null && Sequencer.Sequence.Length > TimeSpan.Zero && ExportFormat != null && !string.IsNullOrWhiteSpace(ExportPath));
			ExportCancelCommand = new DelegateCommand(() => IsExportOpen = false);
			InitializeComponent();
			ExportFormat = ((ExportFormatDescriptor[])Resources["ExportFormatOptions"]).FirstOrDefault();
		}

		public ICommand ZoomInCommand { get; }
		public ICommand ZoomOutCommand { get; }
		public ICommand ExportCommand { get; }
		public ICommand ExportRenderCommand { get; }
		public ICommand ExportCancelCommand { get; }

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

		#region NewFrameRate

		public static readonly DependencyProperty NewFrameRateProperty = DependencyProperty.Register(nameof(NewFrameRate), typeof(int), typeof(MainWindow), new PropertyMetadata(50));

		public int NewFrameRate
		{
			get => (int)GetValue(NewFrameRateProperty);
			set => SetValue(NewFrameRateProperty, value);
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

		public double MinZoom => 0.05;
		public double MaxZoom => 1;

		public static readonly DependencyProperty ZoomProperty = DependencyProperty.Register(nameof(Zoom), typeof(double), typeof(MainWindow), new PropertyMetadata(0.2));

		public double Zoom
		{
			get => (double)GetValue(ZoomProperty);
			set => SetValue(ZoomProperty, value);
		}

		#endregion

		#region DotPitch

		public static readonly DependencyProperty DotPitchProperty = DependencyProperty.Register(nameof(DotPitch), typeof(int), typeof(MainWindow), new PropertyMetadata(2));

		public int DotPitch
		{
			get => (int)GetValue(DotPitchProperty);
			set => SetValue(DotPitchProperty, value);
		}

		#endregion

		#region PixelSize

		public static readonly DependencyProperty PixelSizeProperty = DependencyProperty.Register(nameof(PixelSize), typeof(int), typeof(MainWindow), new PropertyMetadata(5));

		public int PixelSize
		{
			get => (int)GetValue(PixelSizeProperty);
			set => SetValue(PixelSizeProperty, value);
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

		public static readonly DependencyProperty ConfigurationModelProperty = DependencyProperty.Register(nameof(ConfigurationModel), typeof(SequenceStepConfigViewModel), typeof(MainWindow), new PropertyMetadata(null, OnConfigurationModelChanged));

		public SequenceStepConfigViewModel ConfigurationModel
		{
			get => (SequenceStepConfigViewModel)GetValue(ConfigurationModelProperty);
			set => SetValue(ConfigurationModelProperty, value);
		}

		private static void OnConfigurationModelChanged(DependencyObject owner, DependencyPropertyChangedEventArgs e)
		{
			if (e.OldValue is SequenceStepConfigViewModel oldVm) oldVm.Unwire();
		}

		#endregion

		#region IsExportOpen

		public static readonly DependencyProperty IsExportOpenProperty = DependencyProperty.Register(nameof(IsExportOpen), typeof(bool), typeof(MainWindow), new PropertyMetadata(false));

		public bool IsExportOpen
		{
			get => (bool)GetValue(IsExportOpenProperty);
			set => SetValue(IsExportOpenProperty, value);
		}

		#endregion

		#region ExportFormat

		public static readonly DependencyProperty ExportFormatProperty = DependencyProperty.Register(nameof(ExportFormat), typeof(ExportFormatDescriptor), typeof(MainWindow), new PropertyMetadata(null));

		public ExportFormatDescriptor ExportFormat
		{
			get => (ExportFormatDescriptor)GetValue(ExportFormatProperty);
			set => SetValue(ExportFormatProperty, value);
		}

		#endregion

		#region ExportZoom

		public static readonly DependencyProperty ExportZoomProperty = DependencyProperty.Register(nameof(ExportZoom), typeof(int), typeof(MainWindow), new PropertyMetadata(1));

		public int ExportZoom
		{
			get => (int)GetValue(ExportZoomProperty);
			set => SetValue(ExportZoomProperty, value);
		}

		#endregion

		#region ExportPath

		public static readonly DependencyProperty ExportPathProperty = DependencyProperty.Register(nameof(ExportPath), typeof(string), typeof(MainWindow), new PropertyMetadata(null));

		public string ExportPath
		{
			get => (string)GetValue(ExportPathProperty);
			set => SetValue(ExportPathProperty, value);
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
			Sequencer = new SequencerViewModel(this, NewBoardWidth, NewBoardHeight, NewFrameRate);
			IsProjectSettingsOpen = false;
		}

		private void OnCancelClick(object sender, RoutedEventArgs e)
		{
			if (Sequencer == null) Close();
			else IsProjectSettingsOpen = false;
		}

		private void OnZoomOut()
		{
			Zoom = Math.Max(MinZoom, Zoom - 0.05);
		}

		private void OnZoomIn()
		{
			Zoom = Math.Min(MaxZoom, Zoom + 0.05);
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
			if (e.PropertyName == nameof(Sequence.CurrentTime))
			{
				Dispatcher.Invoke(() =>
				{
					// Compute the scroll offset
					var widthConverter = (TimelineWidthConverter)Resources["timelineWidthConverter"];
					double positionOfCaret = (double)widthConverter.Convert(new object[] { Sequencer.Sequence.CurrentTime, Zoom, }, typeof(double), null, null);
					double leftOffsetThreshold = positionOfCaret - timelineScroller.ViewportWidth * 0.9;
					double rightOffsetThreshold = positionOfCaret - timelineScroller.ViewportWidth * 0.1;
					if (timelineScroller.HorizontalOffset < leftOffsetThreshold) timelineScroller.ScrollToHorizontalOffset(leftOffsetThreshold);
					else if (timelineScroller.HorizontalOffset > rightOffsetThreshold) timelineScroller.ScrollToHorizontalOffset(rightOffsetThreshold);
				});
			}

			CommandManager.InvalidateRequerySuggested();
		}

		private void OnToolboxMouseMove(object sender, MouseEventArgs e)
		{
			if (sender is ListBox source && e.LeftButton == MouseButtonState.Pressed)
			{
				DragDrop.DoDragDrop(source, new DataObject(DataFormats.Serializable, source.SelectedItem), DragDropEffects.Copy);
			}
		}

		private void OnTimelineKeyUp(object sender, KeyEventArgs e)
		{
			switch (e.Key)
			{
				case Key.Delete:
					Sequencer?.OnDeleteSelectedItem();
					e.Handled = true;
					break;
			}
		}

		private void OnTimelineControlRequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
		{
			// Don't bother trying to scroll the timeline automatically
			e.Handled = true;
		}

		private void OnExportBrowseClick(object sender, RoutedEventArgs e)
		{
			var sfd = new SaveFileDialog()
			{
				Title = "Export image(s)...",
				DefaultExt = ExportFormat?.DefaultExt,
				Filter = ExportFormat?.Filters,
				OverwritePrompt = true,
			};
			if (sfd.ShowDialog(this) ?? false) ExportPath = sfd.FileName;
		}

		private async void OnExportRender()
		{
			// Validation
			if (Sequencer == null || ExportFormat == null || string.IsNullOrWhiteSpace(ExportPath)) return;

			// Close export dialog
			IsExportOpen = false;

			// Open progress dialog
			var settings = DialogSettings;
			settings.NegativeButtonText = "Cancel";
			var controller = await this.ShowProgressAsync("Please wait...", "Exporting image...", true, settings);

			// Extract properties to locals
			var exportPath = ExportPath;
			var exportZoom = ExportZoom;
			var dotPitch = DotPitch;
			var pixelSize = PixelSize;
			var frameDelay = Sequencer.Sequence.FrameDelay;
			var exportFormat = ExportFormat.Value;

			// Begin export
			using (var tokenSource = new CancellationTokenSource())
			{
				controller.SetIndeterminate();
				controller.Canceled += (sender, e) => tokenSource.Cancel();
				var progress = new ProgressHandler(controller);
				var sequencer = Sequencer;
				await Task.Run(async () =>
				{
					try
					{
						// Determine exporter
						IExportService exporter;
						switch (exportFormat)
						{
							case Models.ExportFormat.GIF:
								exporter = new GifExporter(exportPath, exportZoom, dotPitch, pixelSize, frameDelay);
								break;
							case Models.ExportFormat.PNGSeries:
								exporter = new PngExporter(exportPath, exportZoom, dotPitch, pixelSize);
								break;
							default:
								throw new NotImplementedException($"Unsupported export format: {exportFormat}");
						}

						// Run exporter
						sequencer.Export(progress, exporter, tokenSource.Token);
					}
					catch (Exception ex)
					{
						Dispatcher.Invoke(() =>
						{
							ShowMessageDialog("Error", $"Failed to export image(s): {ex.Message}");
						});
					}
					await controller.CloseAsync();
				});
			}
		}

		#endregion

		#region IDialogService impl

		public async void ShowMessageDialog(string title, string message)
		{
			var settings = DialogSettings;
			settings.AffirmativeButtonText = "OK";
			await this.ShowMessageAsync(title, message, MessageDialogStyle.Affirmative, settings);
		}

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
			var settings = DialogSettings;
			settings.AffirmativeButtonText = affirmativeBtnText ?? "Yes";
			settings.NegativeButtonText = negativeBtnText ?? "No";
			settings.DefaultButtonFocus = MessageDialogResult.Negative;
			var result = await this.ShowMessageAsync(title, message, MessageDialogStyle.AffirmativeAndNegative, settings);
			callback.Invoke(result == MessageDialogResult.Affirmative);
		}

		#endregion

		private MetroDialogSettings DialogSettings => new MetroDialogSettings { AnimateHide = false, AnimateShow = false };

		#region Export handler

		private class ProgressHandler : IProgress<double>
		{
			private readonly ProgressDialogController _Controller;

			public ProgressHandler(ProgressDialogController controller)
			{
				_Controller = controller;
			}

			public void Report(double value)
			{
				_Controller.SetProgress(value);
			}
		}

		#endregion
	}
}
