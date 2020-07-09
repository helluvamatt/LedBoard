using LedBoard.Converters;
using LedBoard.Models;
using LedBoard.Models.Serialization;
using LedBoard.Services;
using LedBoard.Services.Export;
using LedBoard.Services.Resources;
using LedBoard.ViewModels;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;

namespace LedBoard
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : MetroWindow, IDialogService, ICheckDirty
	{
		private readonly ProjectResourcesService _ResourcesService;
		private readonly double[] _ZoomValues;

		private bool _SkipDirty;

		public MainWindow()
		{
			_ResourcesService = new ProjectResourcesService(Path.Combine(Path.GetTempPath(), "LedBoard"));
			SaveProjectCommand = new DelegateCommand(DoSaveProject, () => Sequencer != null);
			ZoomInCommand = new DelegateCommand(OnZoomIn, () => Zoom < MaxZoom);
			ZoomOutCommand = new DelegateCommand(OnZoomOut, () => Zoom > MinZoom);
			ExportCommand = new DelegateCommand(() => IsExportOpen = true, () => Sequencer != null && Sequencer.Sequence.Length > TimeSpan.Zero);
			ExportRenderCommand = new DelegateCommand(OnExportRender, () => Sequencer != null && Sequencer.Sequence.Length > TimeSpan.Zero && ExportFormat != null && !string.IsNullOrWhiteSpace(ExportPath));
			ExportCancelCommand = new DelegateCommand(() => IsExportOpen = false);
			InitializeComponent();
			ExportFormat = ((ExportFormatDescriptor[])Resources["ExportFormatOptions"]).FirstOrDefault();
			_ZoomValues = ((DoubleDescriptor[])Resources["BoardZoomOptions"]).Select(dd => dd.Value).ToArray();
		}

		public ICommand SaveProjectCommand { get; }
		public ICommand ZoomInCommand { get; }
		public ICommand ZoomOutCommand { get; }
		public ICommand ExportCommand { get; }
		public ICommand ExportRenderCommand { get; }
		public ICommand ExportCancelCommand { get; }

		#region Dependency properties

		#region ProjectPath

		public static readonly DependencyProperty ProjectPathProperty = DependencyProperty.Register(nameof(ProjectPath), typeof(string), typeof(MainWindow), new PropertyMetadata(null));

		public string ProjectPath
		{
			get => (string)GetValue(ProjectPathProperty);
			set => SetValue(ProjectPathProperty, value);
		}

		#endregion

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

		#region StepHeight

		public static readonly DependencyProperty StepHeightProperty = DependencyProperty.Register(nameof(StepHeight), typeof(double), typeof(MainWindow), new PropertyMetadata(0.0));

		public double StepHeight
		{
			get => (double)GetValue(StepHeightProperty);
			set => SetValue(StepHeightProperty, value);
		}

		#endregion

		#region TransitionHeight

		public static readonly DependencyProperty TransitionHeightProperty = DependencyProperty.Register(nameof(TransitionHeight), typeof(double), typeof(MainWindow), new PropertyMetadata(0.0));

		public double TransitionHeight
		{
			get => (double)GetValue(TransitionHeightProperty);
			set => SetValue(TransitionHeightProperty, value);
		}

		#endregion

		#region BoardZoom

		public static readonly DependencyProperty BoardZoomProperty = DependencyProperty.Register(nameof(BoardZoom), typeof(double), typeof(MainWindow), new PropertyMetadata(1.0));

		public double BoardZoom
		{
			get => (double)GetValue(BoardZoomProperty);
			set => SetValue(BoardZoomProperty, value);
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

		private async void OnWindowClosing(object sender, CancelEventArgs e)
		{
			if (IsDirty && !_SkipDirty)
			{
				// Cancel now, we will be called again if we are to close again
				e.Cancel = true;

				// Prompt the user to save the project
				bool? result = await ShowConfirmDialogCancelable("Save Project?", "You have unsaved changes to your project. Would you like to save?");
				if (result.HasValue)
				{
					if (result.Value)
					{
						if (await OnSaveProject())
						{
							// No need to set _SkipDirty as the project has been saved and is no longer dirty
							Close();
						}
					}
					else
					{
						_SkipDirty = true;
						Close();
					}
				}
			}
		}

		private void OnProjectSettingsClick(object sender, RoutedEventArgs e)
		{
			IsProjectSettingsOpen = true;
		}

		private async void OnSaveProjectAsClick(object sender, RoutedEventArgs e)
		{
			if (Sequencer == null) return;
			await OnSaveProjectAs();
		}

		private async void OnNewProjectClick(object sender, RoutedEventArgs e)
		{
			if (IsDirty)
			{
				bool? doSave = await ShowConfirmDialogCancelable("Save Project?", "You have unsaved changes to your project. Would you like to save?");
				if ((!doSave.HasValue) || (doSave.Value && !await OnSaveProject())) return;
			}

			Sequencer = new SequencerViewModel(this, _ResourcesService, NewBoardWidth, NewBoardHeight, NewFrameRate);
			IsProjectSettingsOpen = false;
			ProjectPath = null;
		}

		private async void OnLoadProjectClick(object sender, RoutedEventArgs e)
		{
			if (IsDirty)
			{
				bool? doSave = await ShowConfirmDialogCancelable("Save Project?", "You have unsaved changes to your project. Would you like to save?");
				if ((!doSave.HasValue) || (doSave.Value && !await OnSaveProject())) return;
			}

			string result = OpenFileDialog("Open Project...", "LED Board project|*.ledproj|All files|*.*");
			if (result != null)
			{
				// Open progress dialog
				var controller = await this.ShowProgressAsync("Please wait...", "Loading project...", false);
				controller.SetIndeterminate();

				await Task.Run(async () =>
				{
					try
					{
						// Asynchronously load project
						ProjectModel project = new ProjectService(_ResourcesService).LoadProject(result);

						// Back to the UI thread, create the SequencerViewModel from the project
						Dispatcher.Invoke(() =>
						{
							Sequencer = new SequencerViewModel(this, _ResourcesService, project);
							Sequencer.Sequence.GetCurrentFrame(Sequencer.CurrentBoard);
							IsProjectSettingsOpen = false;
							ProjectPath = result;
						});
					}
					catch (Exception ex)
					{
						ShowMessageDialog("Error", $"Failed to load project: {ex.Message}", MessageDialogIconType.Error, ex.ToString());
					}

					await controller.CloseAsync();
				});
			}
		}

		private void OnCancelClick(object sender, RoutedEventArgs e)
		{
			if (Sequencer == null) Close();
			else IsProjectSettingsOpen = false;
		}

		private async void DoSaveProject()
		{
			await OnSaveProject();
		}

		private async Task<bool> OnSaveProject()
		{
			if (ProjectPath == null) return await OnSaveProjectAs();
			else return await SaveProject(ProjectPath);
		}

		private async Task<bool> OnSaveProjectAs()
		{
			if (Sequencer == null) return false;
			string result = SaveFileDialog("Save project as...", "LED Board project|*.ledproj|All files|*.*");
			if (result != null)
			{
				if (await SaveProject(result))
				{
					ProjectPath = result;
					return true;
				}
			}
			return false;
		}

		private async Task<bool> SaveProject(string path)
		{
			// Open progress dialog
			var controller = await this.ShowProgressAsync("Please wait...", "Saving project...", false);
			controller.SetIndeterminate();

			string error = null;
			Exception exception = null;

			try
			{
				// Extract dependency properties to locals
				var sequencer = Sequencer;

				// Create project model
				var project = sequencer.ExportProject();

				// Asynchrounously save the project
				await Task.Run(() =>
				{
					var resourceList = new List<ProjectResourceModel>();
					var errorList = new List<string>();
					foreach (var entry in sequencer.Sequence.Steps)
					{
						foreach (var resourceUri in entry.Step.Resources.Where(uri => !string.IsNullOrWhiteSpace(uri)))
						{
							if (_ResourcesService.TryGetResourceMeta(resourceUri, out long filesize, out string signature))
							{
								resourceList.Add(new ProjectResourceModel
								{
									Uri = resourceUri,
									FileSize = filesize,
									Signature = signature,
								});
							}
							else
							{
								errorList.Add($"{entry.Step.DisplayName}: {resourceUri}");
							}
						}
					}

					if (errorList.Any())
					{
						// Resource processing failed
						error = $"Some resources are invalid:\r\n\r\n{string.Join("\r\n", errorList)}";
					}
					else
					{
						// Store resource references in project
						project.Resources = resourceList.ToArray();

						// Save project
						new ProjectService(_ResourcesService).SaveProject(project, path);
					}
				});
			}
			catch (Exception ex)
			{
				error = ex.Message;
				exception = ex;
			}

			await controller.CloseAsync();

			if (error != null || exception != null)
			{
				ShowMessageDialog("Error", $"Failed to save project: {error ?? exception.Message}", MessageDialogIconType.Error, exception?.ToString());
				return false;
			}

			Sequencer.Sequence.ResetDirty();
			return true;
		}

		private void OnZoomOut()
		{
			Zoom = Math.Max(MinZoom, Zoom - 0.05);
		}

		private void OnZoomIn()
		{
			Zoom = Math.Min(MaxZoom, Zoom + 0.05);
		}

		private void OnBoardContainerMouseWheel(object sender, MouseWheelEventArgs e)
		{
			int index = Array.IndexOf(_ZoomValues, BoardZoom);
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
			BoardZoom = _ZoomValues[index];
			e.Handled = true;
		}

		private void OnSelectedItemChanged(object sender, EventArgs e)
		{
			if (Sequencer?.SelectedItem != null)
			{
				ConfigurationModel = new SequenceStepConfigViewModel(Sequencer.SelectedItem, this, _ResourcesService);
				ToolboxTabPage = 2;
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
			else if (e.PropertyName == nameof(Sequence.IsDirty))
			{
				Dispatcher.Invoke(() =>
				{
					if (IsDirty)
					{
						Interop.User32.ShutdownBlockReasonCreate(new WindowInteropHelper(this).Handle, "You have unsaved changes to your project.");
					}
					else
					{
						Interop.User32.ShutdownBlockReasonDestroy(new WindowInteropHelper(this).Handle);
					}
				});
			}

			CommandManager.InvalidateRequerySuggested();
		}

		private void OnToolboxMouseMove(object sender, MouseEventArgs e)
		{
			if (sender is ListBox source && e.LeftButton == MouseButtonState.Pressed && source.SelectedItem != null)
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
			var settings = new MetroDialogSettings
			{
				AnimateShow = false,
				AnimateHide = false,
				NegativeButtonText = "Cancel"
			};
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
							ShowMessageDialog("Error", $"Failed to export image(s): {ex.Message}", MessageDialogIconType.Error, ex.ToString());
						});
					}
					await controller.CloseAsync();
				});
			}
		}

		#endregion

		#region IDialogService impl

		private BaseMetroDialog _CurrentDialog;

		public void ShowMessageDialog(string title, string message, MessageDialogIconType icon, string detailedMessage = null)
		{
			Dispatcher.Invoke(async () =>
			{
				var vm = new MessageDialogViewModel
				{
					Title = title,
					Message = message,
					DetailedMessage = detailedMessage,
					IconType = icon,
				};
				_CurrentDialog = (BaseMetroDialog)Resources["MessageDialog"];
				_CurrentDialog.DataContext = vm;
				await this.ShowMetroDialogAsync(_CurrentDialog);
			});
		}

		public string OpenFileDialog(string title, string filters, string initialDirectory = null)
		{
			return Dispatcher.Invoke(() =>
			{
				var ofd = new OpenFileDialog
				{
					CheckFileExists = true,
					Filter = filters,
					InitialDirectory = initialDirectory,
					Title = title,
				};
				return (ofd.ShowDialog(this) ?? false) ? ofd.FileName : null;
			});
		}

		public string SaveFileDialog(string title, string filters, string initialDirectory = null)
		{
			return Dispatcher.Invoke(() =>
			{
				var sfd = new SaveFileDialog
				{
					Filter = filters,
					InitialDirectory = initialDirectory,
					Title = title,
				};
				return (sfd.ShowDialog(this) ?? false) ? sfd.FileName : null;
			});
		}

		public void ConfirmDialog(string title, string message, Action<bool> callback, string affirmativeBtnText = null, string negativeBtnText = null)
		{
			Dispatcher.Invoke(async () =>
			{
				bool result = await ShowConfirmDialog(title, message, affirmativeBtnText, negativeBtnText);
				callback.Invoke(result);
			});
		}

		private async void OnCloseMessageDialog(object sender, RoutedEventArgs e)
		{
			if (_CurrentDialog != null)
			{
				await this.HideMetroDialogAsync(_CurrentDialog);
				_CurrentDialog = null;
			}
		}

		private async Task<bool> ShowConfirmDialog(string title, string message, string affirmativeBtnText = null, string negativeBtnText = null)
		{
			var settings = new MetroDialogSettings
			{
				AnimateShow = false,
				AnimateHide = false,
				AffirmativeButtonText = affirmativeBtnText ?? "Yes",
				NegativeButtonText = negativeBtnText ?? "No",
				DefaultButtonFocus = MessageDialogResult.Negative
			};
			return (await this.ShowMessageAsync(title, message, MessageDialogStyle.AffirmativeAndNegative, settings)) == MessageDialogResult.Affirmative;
		}

		private async Task<bool?> ShowConfirmDialogCancelable(string title, string message, string affirmativeBtnText = null, string negativeBtnText = null, string auxBtnText = null)
		{
			var settings = new MetroDialogSettings
			{
				AnimateShow = false,
				AnimateHide = false,
				AffirmativeButtonText = affirmativeBtnText ?? "Yes",
				NegativeButtonText = negativeBtnText ?? "No",
				FirstAuxiliaryButtonText = auxBtnText ?? "Cancel",
				DefaultButtonFocus = MessageDialogResult.FirstAuxiliary,
			};
			var result = await this.ShowMessageAsync(title, message, MessageDialogStyle.AffirmativeAndNegativeAndSingleAuxiliary, settings);
			if (result == MessageDialogResult.FirstAuxiliary) return null;
			return result == MessageDialogResult.Affirmative;
		}

		#endregion

		#region ICheckDirty impl

		public bool IsDirty => Sequencer?.Sequence?.IsDirty ?? false;

		public async void HandleSessionEnd()
		{
			bool? doSave = await ShowConfirmDialogCancelable("Save Project?", "You have unsaved changes to your project. Would you like to save?");
			if ((!doSave.HasValue) || (doSave.Value && !await OnSaveProject())) return;
		}

		#endregion

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
