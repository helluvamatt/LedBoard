using LedBoard.Interop;
using LedBoard.Models;
using LedBoard.Models.Serialization;
using LedBoard.Properties;
using LedBoard.Services;
using LedBoard.Services.Export;
using LedBoard.Services.Resources;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace LedBoard.ViewModels
{
	public class ShellViewModel : DependencyObject, ICheckDirty
	{
		private readonly IDialogService _DialogService;
		private readonly ProjectResourcesService _ResourcesService;

		private FullscreenWindow _FullscreenWindow;

		public ShellViewModel(IDialogService dialogService, ProjectResourcesService resourceService)
		{
			_ResourcesService = resourceService;
			_DialogService = dialogService;
			Navigate("Views/ProjectSettingsPage.xaml");

			NavigateProjectSettingsCommand = new DelegateCommand(() => Navigate("Views/ProjectSettingsPage.xaml"));
			NavigateProjectEditorCommand = new DelegateCommand(() => Navigate("Views/ProjectPage.xaml"), () => Sequencer != null);
			NavigateViewerCommand = new DelegateCommand(() => Navigate("Views/ViewerPage.xaml"), () => Sequencer != null);
			NavigateExportCommand = new DelegateCommand(() => Navigate("Views/ExportPage.xaml"), () => Sequencer != null);
			NavigateSettingsCommand = new DelegateCommand(() => Navigate("Views/SettingsPage.xaml"));

			NavigateNewProjectCommand = new DelegateCommand(OnNavigateNewProject);
			NewProjectCommand = new DelegateCommand(OnNewProject);
			LoadProjectCommand = new DelegateCommand(OnLoadProject);
			SaveProjectCommand = new DelegateCommand(async () => await OnSaveProject(), () => Sequencer != null && (Sequencer.Sequence.IsDirty || IsConfigurationDifferent()));
			SaveProjectAsCommand = new DelegateCommand(async () => await OnSaveProjectAs(), () => Sequencer != null);
			OpenRecentCommand = new DelegateCommand<string>(OnLoadProject);
			
			AddAssociationCommand = new DelegateCommand(OnAddAssociation);
			RemoveAssociationCommand = new DelegateCommand(OnRemoveAssociation);
			
			ZoomInCommand = new DelegateCommand(OnZoomIn, () => Settings.Default.TimelineZoom < MaxZoom);
			ZoomOutCommand = new DelegateCommand(OnZoomOut, () => Settings.Default.TimelineZoom > MinZoom);
			DeleteSelectedItemCommand = new DelegateCommand(OnDeleteSelectedItem, () => Sequencer?.SelectedItem != null);
			
			ExportBrowseCommand = new DelegateCommand(OnExportBrowse, () => Sequencer != null && Sequencer.Sequence.Length > TimeSpan.Zero);
			ExportRenderCommand = new DelegateCommand(OnExportRender, () => Sequencer != null && Sequencer.Sequence.Length > TimeSpan.Zero && ExportFormat != null && !string.IsNullOrWhiteSpace(ExportPath));
			
			Settings.Default.PropertyChanged += OnSettingsPropertyChanged;

			// Get monitors for fullscreen
			Monitors = new ObservableCollection<MonitorInfo>();
			foreach (var monitor in User32.GetMonitors())
			{
				Monitors.Add(monitor);
			}

			// Default setting to primary monitor
			if (Monitors.FirstOrDefault(mi => mi.DeviceName == Settings.Default.FullscreenMonitorName) == null)
			{
				Settings.Default.FullscreenMonitorName = Monitors.FirstOrDefault(mi => mi.IsPrimary)?.DeviceName;
			}
		}

		public async void LoadProjectOnStartup(string project)
		{
			await LoadProject(project);
		}

		#region Commands

		#region Navigation commands

		public ICommand NavigateProjectSettingsCommand { get; }
		public ICommand NavigateNewProjectCommand { get; }
		public ICommand NavigateProjectEditorCommand { get; }
		public ICommand NavigateViewerCommand { get; }
		public ICommand NavigateExportCommand { get; }
		public ICommand NavigateSettingsCommand { get; }

		#endregion

		#region Settings commands

		public ICommand NewProjectCommand { get; }
		public ICommand LoadProjectCommand { get; }
		public ICommand SaveProjectCommand { get; }
		public ICommand SaveProjectAsCommand { get; }
		public ICommand OpenRecentCommand { get; }
		public ICommand AddAssociationCommand { get; }
		public ICommand RemoveAssociationCommand { get; }

		#endregion

		#region Project Editor commands

		public ICommand ZoomInCommand { get; }
		public ICommand ZoomOutCommand { get; }
		public ICommand DeleteSelectedItemCommand { get; }

		#endregion

		#region Export commands

		public ICommand ExportBrowseCommand { get; }
		public ICommand ExportRenderCommand { get; }

		#endregion

		#endregion

		public event PropertyChangedEventHandler SequencePropertyChanged;

		public double MinZoom => 0.05;
		public double MaxZoom => 1;

		public ObservableCollection<MonitorInfo> Monitors { get; }

		#region Dependency properties

		#region CurrentPage

		public static readonly DependencyProperty CurrentPageProperty = DependencyProperty.Register(nameof(CurrentPage), typeof(Uri), typeof(ShellViewModel), new PropertyMetadata(null));

		public Uri CurrentPage
		{
			get => (Uri)GetValue(CurrentPageProperty);
			set => SetValue(CurrentPageProperty, value);
		}

		#endregion

		#region ProjectPath

		public static readonly DependencyProperty ProjectPathProperty = DependencyProperty.Register(nameof(ProjectPath), typeof(string), typeof(ShellViewModel), new PropertyMetadata(null));

		public string ProjectPath
		{
			get => (string)GetValue(ProjectPathProperty);
			set => SetValue(ProjectPathProperty, value);
		}

		#endregion

		#region Sequencer

		public static readonly DependencyProperty SequencerProperty = DependencyProperty.Register(nameof(Sequencer), typeof(SequencerViewModel), typeof(ShellViewModel), new PropertyMetadata(null, OnSequencerChange));

		public SequencerViewModel Sequencer
		{
			get => (SequencerViewModel)GetValue(SequencerProperty);
			set => SetValue(SequencerProperty, value);
		}

		private static void OnSequencerChange(DependencyObject owner, DependencyPropertyChangedEventArgs e)
		{
			var vm = (ShellViewModel)owner;
			if (e.OldValue is SequencerViewModel oldSequencer)
			{
				oldSequencer.SelectedItemChanged -= vm.OnSelectedItemChanged;
				oldSequencer.Sequence.PropertyChanged -= vm.OnSequencePropertyChanged;
			}
			if (e.NewValue is SequencerViewModel newSequencer)
			{
				newSequencer.Sequence.Loop = Settings.Default.IsLooping;
				newSequencer.Sequence.PropertyChanged += vm.OnSequencePropertyChanged;
				newSequencer.SelectedItemChanged += vm.OnSelectedItemChanged;

				// Inform application of IsDirty status immediately
				vm.OnSequencePropertyChanged(vm, new PropertyChangedEventArgs(nameof(Sequence.IsDirty)));
			}
		}

		#endregion

		#region StepHeight

		public static readonly DependencyProperty StepHeightProperty = DependencyProperty.Register(nameof(StepHeight), typeof(double), typeof(ShellViewModel), new PropertyMetadata(0.0));

		public double StepHeight
		{
			get => (double)GetValue(StepHeightProperty);
			set => SetValue(StepHeightProperty, value);
		}

		#endregion

		#region TransitionHeight

		public static readonly DependencyProperty TransitionHeightProperty = DependencyProperty.Register(nameof(TransitionHeight), typeof(double), typeof(ShellViewModel), new PropertyMetadata(0.0));

		public double TransitionHeight
		{
			get => (double)GetValue(TransitionHeightProperty);
			set => SetValue(TransitionHeightProperty, value);
		}

		#endregion

		#region ToolboxTabPage

		public static readonly DependencyProperty ToolboxTabPageProperty = DependencyProperty.Register(nameof(ToolboxTabPage), typeof(int), typeof(ShellViewModel), new PropertyMetadata(0));

		public int ToolboxTabPage
		{
			get => (int)GetValue(ToolboxTabPageProperty);
			set => SetValue(ToolboxTabPageProperty, value);
		}

		#endregion

		#region ConfigurationModel

		public static readonly DependencyProperty ConfigurationModelProperty = DependencyProperty.Register(nameof(ConfigurationModel), typeof(SequenceStepConfigViewModel), typeof(ShellViewModel), new PropertyMetadata(null, OnConfigurationModelChanged));

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

		#region ExportFormat

		public static readonly DependencyProperty ExportFormatProperty = DependencyProperty.Register(nameof(ExportFormat), typeof(ExportFormatDescriptor), typeof(ShellViewModel), new PropertyMetadata(null));

		public ExportFormatDescriptor ExportFormat
		{
			get => (ExportFormatDescriptor)GetValue(ExportFormatProperty);
			set => SetValue(ExportFormatProperty, value);
		}

		#endregion

		#region ExportZoom

		public static readonly DependencyProperty ExportZoomProperty = DependencyProperty.Register(nameof(ExportZoom), typeof(int), typeof(ShellViewModel), new PropertyMetadata(1));

		public int ExportZoom
		{
			get => (int)GetValue(ExportZoomProperty);
			set => SetValue(ExportZoomProperty, value);
		}

		#endregion

		#region ExportPath

		public static readonly DependencyProperty ExportPathProperty = DependencyProperty.Register(nameof(ExportPath), typeof(string), typeof(ShellViewModel), new PropertyMetadata(null));

		public string ExportPath
		{
			get => (string)GetValue(ExportPathProperty);
			set => SetValue(ExportPathProperty, value);
		}

		#endregion

		#region FullscreenVisible

		public static readonly DependencyProperty FullscreenVisibleProperty = DependencyProperty.Register(nameof(FullscreenVisible), typeof(bool), typeof(ShellViewModel), new PropertyMetadata(false, OnFullscreenVisibleChanged));

		public bool FullscreenVisible
		{
			get => (bool)GetValue(FullscreenVisibleProperty);
			set => SetValue(FullscreenVisibleProperty, value);
		}

		private static void OnFullscreenVisibleChanged(DependencyObject owner, DependencyPropertyChangedEventArgs e)
		{
			var vm = (ShellViewModel)owner;
			if (vm.FullscreenVisible) vm.OnShowFullscreen();
			else vm.OnHideFullscreen();
		}

		private void OnShowFullscreen()
		{
			if (_FullscreenWindow == null)
			{
				var selectedMonitor = Monitors.FirstOrDefault(mi => mi.DeviceName == Settings.Default.FullscreenMonitorName);	
				if (selectedMonitor != null)
				{
					var vm = new FullscreenViewModel(Sequencer);
					_FullscreenWindow = new FullscreenWindow(selectedMonitor.Left, selectedMonitor.Top, selectedMonitor.Width, selectedMonitor.Height)
					{
						DataContext = vm,
					};
				}
			}
			_FullscreenWindow?.Show();
		}

		private void OnHideFullscreen()
		{
			_FullscreenWindow?.Close();
			_FullscreenWindow = null;
		}

		#endregion

		#endregion

		#region Event handlers

		private void OnSettingsPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(Settings.IsLooping))
			{
				if (Sequencer != null) Sequencer.Sequence.Loop = Settings.Default.IsLooping;
			}
		}

		private void OnSequencePropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			SequencePropertyChanged?.Invoke(sender, e);
		}

		private void Navigate(string uri)
		{
			CurrentPage = new Uri(uri, UriKind.RelativeOrAbsolute);
		}

		private async void OnNavigateNewProject()
		{
			if (IsDirty)
			{
				bool? doSave = await _DialogService.ShowConfirmDialogCancelable("Save Project?", "You have unsaved changes to your project. Would you like to save?");
				if ((!doSave.HasValue) || (doSave.Value && !await OnSaveProject())) return;
			}

			Navigate("Views/NewProjectPage.xaml");
		}

		private void OnNewProject()
		{
			Sequencer = new SequencerViewModel(_DialogService, _ResourcesService, Settings.Default.NewBoardWidth, Settings.Default.NewBoardHeight, Settings.Default.NewFrameRate);
			Navigate("Views/ProjectPage.xaml");
			ProjectPath = null;
		}

		private async void OnLoadProject()
		{
			if (IsDirty)
			{
				bool? doSave = await _DialogService.ShowConfirmDialogCancelable("Save Project?", "You have unsaved changes to your project. Would you like to save?");
				if ((!doSave.HasValue) || (doSave.Value && !await OnSaveProject())) return;
			}

			string result = _DialogService.OpenFileDialog("Open Project...", "LED Board project|*.ledproj|All files|*.*");
			if (result != null)
			{
				await LoadProject(result);
			}
		}

		private async void OnLoadProject(string projectPath)
		{
			if (IsDirty)
			{
				bool? doSave = await _DialogService.ShowConfirmDialogCancelable("Save Project?", "You have unsaved changes to your project. Would you like to save?");
				if ((!doSave.HasValue) || (doSave.Value && !await OnSaveProject())) return;
			}

			await LoadProject(projectPath);
		}

		private async Task LoadProject(string projectPath)
		{
			// Open progress dialog
			var controller = await _DialogService.ShowProgressDialogAsync("Please wait...", "Loading project...", false);
			controller.SetIndeterminate();

			await Task.Run(async () =>
			{
				try
				{
					// Asynchronously load project
					ProjectModel project = new ProjectService(_ResourcesService).LoadProject(projectPath);

					// Back to the UI thread, create the SequencerViewModel from the project
					Dispatcher.Invoke(() =>
					{
						Sequencer = new SequencerViewModel(_DialogService, _ResourcesService, project);
						Sequencer.Sequence.GetCurrentFrame(Sequencer.CurrentBoard);
						ProjectPath = projectPath;
						ResetConfigurationToSequencer();
						Navigate("Views/ProjectPage.xaml");
					});
				}
				catch (Exception ex)
				{
					_DialogService.ShowMessageDialog("Error", $"Failed to load project: {ex.Message}", MessageDialogIconType.Error, ex.ToString());
				}

				await controller.CloseAsync();
			});
		}

		private async Task<bool> OnSaveProject()
		{
			if (Sequencer == null) return false;
			ConfigureSequencerIfNeeded();
			bool result;
			if (ProjectPath == null) result = await OnSaveProjectAs();
			else result = await SaveProject(ProjectPath);
			ResetConfigurationToSequencer();
			return result;
		}

		private async Task<bool> OnSaveProjectAs()
		{
			if (Sequencer == null) return false;
			ConfigureSequencerIfNeeded();
			string result = _DialogService.SaveFileDialog("Save project as...", "LED Board project|*.ledproj|All files|*.*");
			if (result != null)
			{
				if (await SaveProject(result))
				{
					ProjectPath = result;
					ResetConfigurationToSequencer();
					return true;
				}
			}
			return false;
		}

		private async Task<bool> SaveProject(string path)
		{
			// Open progress dialog
			var controller = await _DialogService.ShowProgressDialogAsync("Please wait...", "Saving project...", false);
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
				_DialogService.ShowMessageDialog("Error", $"Failed to save project: {error ?? exception.Message}", MessageDialogIconType.Error, exception?.ToString());
				return false;
			}

			Sequencer.Sequence.ResetDirty();
			return true;
		}

		private void OnAddAssociation()
		{
			Task.Run(() => RunAssociation(false));
		}

		private void OnRemoveAssociation()
		{
			Task.Run(() => RunAssociation(true));
		}

		private void RunAssociation(bool remove)
		{
			var lines = new List<string>();
			bool hasError = false;
			try
			{
				string pipeName = $"LedBoard_{Guid.NewGuid()}";
				using (var s = new NamedPipeServerStream(pipeName, PipeDirection.In))
				{
					StringBuilder argumentBuilder = new StringBuilder();
					if (remove) argumentBuilder.Append("--remove ");
					argumentBuilder.Append($"--pipe \"{pipeName}\"");

					var psi = new ProcessStartInfo("LedBoard.RegistryTool.exe")
					{
						Verb = "runas",
						UseShellExecute = true,
						WindowStyle = ProcessWindowStyle.Hidden,
						Arguments = argumentBuilder.ToString()
					};
					using (var process = Process.Start(psi))
					{
						s.WaitForConnection();
						using (var reader = new StreamReader(s))
						{
							string line;
							while ((line = reader.ReadLine()) != null)
							{
								if (line.StartsWith("E:"))
								{
									lines.Add(line.Substring(2));
									hasError = true;
								}
								else lines.Add(line);
							}
						}
						process.WaitForExit();
					}
				}
			}
			catch (Exception ex)
			{
				lines.Add($"Failed to run association utility: {ex.Message}");
				hasError = true;
			}
			string message;
			if (remove)
			{
				if (hasError) message = "There was a problem removing the project association from the registry.";
				else message = "Successfully removed the project association from the registry.";
			}
			else
			{
				if (hasError) message = "There was a problem setting the project association in the registry.";
				else message = "Successfully set the project associatiion in the registry.";
			}
			_DialogService.ShowMessageDialog(hasError ? "Error" : "Success", message, hasError ? MessageDialogIconType.Error : MessageDialogIconType.Info, string.Join("\r\n", lines));
		}

		private void OnZoomOut()
		{
			Settings.Default.TimelineZoom = Math.Max(MinZoom, Settings.Default.TimelineZoom - 0.05);
		}

		private void OnZoomIn()
		{
			Settings.Default.TimelineZoom = Math.Min(MaxZoom, Settings.Default.TimelineZoom + 0.05);
		}

		private void OnSelectedItemChanged(object sender, EventArgs e)
		{
			if (Sequencer?.SelectedItem != null)
			{
				ConfigurationModel = new SequenceStepConfigViewModel(Sequencer.SelectedItem, _DialogService, _ResourcesService);
				ToolboxTabPage = 2;
			}
			else
			{
				ConfigurationModel = null;
			}
		}

		private void OnDeleteSelectedItem()
		{
			Sequencer?.OnDeleteSelectedItem();
		}

		private void OnExportBrowse()
		{
			string result = _DialogService.SaveFileDialog("Export image(s)...", ExportFormat?.Filters, ExportFormat?.DefaultExt);
			if (!string.IsNullOrWhiteSpace(result)) ExportPath = result;
		}

		private async void OnExportRender()
		{
			// Validation
			if (Sequencer == null || ExportFormat == null || string.IsNullOrWhiteSpace(ExportPath)) return;

			// Open progress dialog
			var controller = await _DialogService.ShowProgressDialogAsync("Please wait...", "Exporting image...", true);

			// Extract properties to locals
			var exportPath = ExportPath;
			var exportZoom = ExportZoom;
			var dotPitch = Settings.Default.DotPitch;
			var pixelSize = Settings.Default.PixelSize;
			var minPixelBrightness = Settings.Default.MinPixelBrightness;
			var frameDelay = Sequencer.Sequence.FrameDelay;
			var exportFormat = ExportFormat.Value;

			// Begin export
			using (var tokenSource = new CancellationTokenSource())
			{
				controller.SetIndeterminate();
				controller.Canceled += (sender, e) => tokenSource.Cancel();
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
								exporter = new GifExporter(exportPath, exportZoom, dotPitch, pixelSize, minPixelBrightness, frameDelay);
								break;
							case Models.ExportFormat.PNGSeries:
								exporter = new PngExporter(exportPath, exportZoom, dotPitch, pixelSize, minPixelBrightness);
								break;
							case Models.ExportFormat.APNG:
								exporter = new ApngExporter(exportPath, exportZoom, dotPitch, pixelSize, minPixelBrightness, frameDelay);
								break;
							default:
								throw new NotImplementedException($"Unsupported export format: {exportFormat}");
						}

						// Run exporter
						sequencer.Export(controller, exporter, tokenSource.Token);
					}
					catch (Exception ex)
					{
						_DialogService.ShowMessageDialog("Error", $"Failed to export image(s): {ex.Message}", MessageDialogIconType.Error, ex.ToString());
					}
					await controller.CloseAsync();
				});
			}
		}

		private bool IsConfigurationDifferent() => Sequencer.Sequence.BoardWidth != Settings.Default.NewBoardWidth || Sequencer.Sequence.BoardHeight != Settings.Default.NewBoardHeight || Sequencer.Sequence.FrameDelay.TotalMilliseconds != Settings.Default.NewFrameRate;

		private void ConfigureSequencerIfNeeded()
		{
			if (IsConfigurationDifferent())
			{
				Sequencer.Configure(Settings.Default.NewBoardWidth, Settings.Default.NewBoardHeight, Settings.Default.NewFrameRate);
			}
		}

		private void ResetConfigurationToSequencer()
		{
			Settings.Default.NewBoardWidth = Sequencer.Sequence.BoardWidth;
			Settings.Default.NewBoardHeight = Sequencer.Sequence.BoardHeight;
			Settings.Default.NewFrameRate = (int)Sequencer.Sequence.FrameDelay.TotalMilliseconds;
		}

		#endregion

		#region ICheckDirty impl

		public bool IsDirty => Sequencer?.Sequence?.IsDirty ?? false;

		public async void HandleSessionEnd()
		{
			bool? doSave = await _DialogService.ShowConfirmDialogCancelable("Save Project?", "You have unsaved changes to your project. Would you like to save?");
			if ((!doSave.HasValue) || (doSave.Value && !await OnSaveProject())) return;
		}

		#endregion
	}
}
