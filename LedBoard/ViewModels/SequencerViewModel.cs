using LedBoard.Models;
using LedBoard.Models.Serialization;
using LedBoard.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace LedBoard.ViewModels
{
	public class SequencerViewModel : DependencyObject
	{
		private readonly IDialogService _DialogService;

		private CancellationTokenSource _CancelController;

		public SequencerViewModel(IDialogService dialogService, IResourcesService resourcesService, ProjectModel project) : this(dialogService, resourcesService, project.Width, project.Height, project.FrameDelay)
		{
			// Populate sequencer/sequence from project
			int index = 0;
			try
			{
				foreach (ProjectStepModel step in project.Steps)
				{
					index++;
					ISequenceStep sequenceStep = StepService.CreateStep(step);
					var entry = new SequenceEntry(sequenceStep);
					if (step.Transition != null) entry.Transition = StepService.CreateTransition(step.Transition);
					Sequence.Steps.Add(entry);
				}
			}
			catch (Exception ex)
			{
				throw new InvalidDataException($"Sequence Step #{index} is either an unknown type or its configuration was invalid.", ex);
			}
			Sequence.IsDirty = false;
		}

		public SequencerViewModel(IDialogService dialogService, IResourcesService resourcesService, int boardWidth, int boardHeight, int frameDelay)
		{
			_DialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
			if (resourcesService == null) throw new ArgumentNullException(nameof(resourcesService));
			Sequence = new Sequence(resourcesService)
			{
				IsDirty = true,
			};
			Sequence.CurrentFrameChanged += OnSequenceCurrentFrameChanged;
			Configure(boardWidth, boardHeight, frameDelay);
			StopCommand = new DelegateCommand(OnStop);
			PlayPauseCommand = new DelegateCommand(OnPlayPause);
			StepForwardCommand = new DelegateCommand(OnStepForward, () => !IsPlaying);
			StepBackwardCommand = new DelegateCommand(OnStepBackward, () => !IsPlaying);
			AddItemCommand = new DelegateCommand(OnAddItem, () => !IsPlaying && SelectedStepType != null);
			DeleteItemCommand = new DelegateCommand(OnDeleteSelectedItem, () => !IsPlaying && SelectedItem != null);
		}

		public Sequence Sequence { get; }

		public IEnumerable<StepDescriptor> StepTypes => StepService.StepTypes;
		public IEnumerable<TransitionDescriptor> TransitionTypes => StepService.TransitionTypes;

		public void Configure(int boardWidth, int boardHeight, int frameDelay)
		{
			PauseSequence();
			CurrentBoard = new MemoryBoard(boardWidth, boardHeight);
			Sequence.Configure(boardWidth, boardHeight, frameDelay);
		}

		#region Dependency properties

		#region CurrentBoard

		public static readonly DependencyProperty CurrentBoardProperty = DependencyProperty.Register(nameof(CurrentBoard), typeof(IBoard), typeof(SequencerViewModel), new PropertyMetadata(null));

		public IBoard CurrentBoard
		{
			get => (IBoard)GetValue(CurrentBoardProperty);
			private set => SetValue(CurrentBoardProperty, value);
		}

		#endregion

		#region IsPlaying

		public static readonly DependencyProperty IsPlayingProperty = DependencyProperty.Register(nameof(IsPlaying), typeof(bool), typeof(SequencerViewModel), new PropertyMetadata(false, OnIsPlayingChanged));

		public bool IsPlaying
		{
			get => (bool)GetValue(IsPlayingProperty);
			set => SetValue(IsPlayingProperty, value);
		}

		private static void OnIsPlayingChanged(DependencyObject owner, DependencyPropertyChangedEventArgs e)
		{
			CommandManager.InvalidateRequerySuggested();
		}

		#endregion

		#region SelectedStepType

		public static readonly DependencyProperty SelectedStepTypeProperty = DependencyProperty.Register(nameof(SelectedStepType), typeof(StepDescriptor), typeof(SequencerViewModel), new PropertyMetadata(null));

		public StepDescriptor SelectedStepType
		{
			get => (StepDescriptor)GetValue(SelectedStepTypeProperty);
			set => SetValue(SelectedStepTypeProperty, value);
		}

		#endregion

		#region SelectedItem

		public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register(nameof(SelectedItem), typeof(ISequenceItem), typeof(SequencerViewModel), new PropertyMetadata(null, OnSelectedItemChanged));

		public ISequenceItem SelectedItem
		{
			get => (ISequenceItem)GetValue(SelectedItemProperty);
			set => SetValue(SelectedItemProperty, value);
		}

		private static void OnSelectedItemChanged(DependencyObject owner, DependencyPropertyChangedEventArgs e)
		{
			var self = (SequencerViewModel)owner;
			self.SelectedItemChanged?.Invoke(self, EventArgs.Empty);
		}

		public event EventHandler SelectedItemChanged;

		#endregion

		#endregion

		#region Commands

		#region Stop

		public ICommand StopCommand { get; }

		private void OnStop()
		{
			if (IsPlaying) PauseSequence();
			Sequence.CurrentTime = 0;
		}

		#endregion

		#region PlayPause

		public ICommand PlayPauseCommand { get; }

		private void OnPlayPause()
		{
			if (IsPlaying)
			{
				PauseSequence();
			}
			else
			{
				PlaySequence();
			}
		}

		#endregion

		#region StepForward

		public ICommand StepForwardCommand { get; }

		private void OnStepForward()
		{
			if (IsPlaying) return;
			Sequence.StepForward();
		}

		#endregion

		#region StepBackward

		public ICommand StepBackwardCommand { get; }

		private void OnStepBackward()
		{
			if (IsPlaying) return;
			Sequence.StepBackward();
		}

		#endregion

		#region AddItem

		public ICommand AddItemCommand { get; }

		private void OnAddItem()
		{
			if (SelectedStepType == null) return;

			var step = StepService.CreateStep(SelectedStepType);
			var entry = new SequenceEntry(step);

			int index;
			if (SelectedItem != null) FindEntryForItem(SelectedItem, out index);
			else index = -1;
			if (index < 0) Sequence.Steps.Add(entry);
			else Sequence.Steps.Insert(index, entry);
		}

		#endregion

		#region DeleteItem

		public ICommand DeleteItemCommand { get; }

		public void OnDeleteSelectedItem()
		{
			if (SelectedItem == null) return;

			_DialogService.ConfirmDialog("Are you sure you want to delete this item?", SelectedItem.DisplayName, result =>
			{
				if (result)
				{
					var entry = FindEntryForItem(SelectedItem, out int index);
					if (entry != null)
					{
						if (SelectedItem is ISequenceStep)
						{
							Sequence.Steps.Remove(entry);
							SelectedItem = null;
						}
						else if (SelectedItem is ISequenceTransition)
						{
							entry.Transition = null;
							SelectedItem = entry.Step;
						}
					}
				}
			});
		}

		#endregion

		#endregion

		private SequenceEntry FindEntryForItem(ISequenceItem item, out int entryIndex)
		{
			for (int i = 0; i < Sequence.Steps.Count; i++)
			{
				var cur = Sequence.Steps[i];
				if (cur.Step == item || cur.Transition == item)
				{
					entryIndex = i;
					return cur;
				}
			}
			entryIndex = -1;
			return null;
		}

		private void OnSequenceCurrentFrameChanged(object sender, EventArgs e)
		{
			Dispatcher.Invoke(() => Sequence.GetCurrentFrame(CurrentBoard));
		}

		private void PauseSequence()
		{
			if (_CancelController != null) _CancelController.Cancel();
			_CancelController = null;
			IsPlaying = false;
		}

		private void PlaySequence()
		{
			_CancelController = new CancellationTokenSource();
			IsPlaying = true;
			Task.Run(Playing);
		}

		private async void Playing()
		{
			bool hasMore;
			DateTime start;
			DateTime end;
			TimeSpan renderDelay;
			while (_CancelController != null && !_CancelController.IsCancellationRequested)
			{
				start = DateTime.Now;
				hasMore = Sequence.Advance();
				end = DateTime.Now;
				renderDelay = Sequence.FrameDelay - (end - start);
				if (hasMore || Sequence.Loop)
				{
					if (renderDelay > TimeSpan.Zero)
					{
						try
						{
							await Task.Delay(renderDelay, _CancelController.Token);
						}
						catch (OperationCanceledException) { } // Eat, we are pausing
					}
					if (!hasMore && Sequence.Loop) Sequence.CurrentTime = 0;
				}
				else break;
			}
			Dispatcher.Invoke(PauseSequence);
		}

		#region Export

		public void Export(IProgress<double> progress, IExportService exporter, CancellationToken cancelToken)
		{
			try
			{
				var board = new MemoryBoard(Sequence.BoardWidth, Sequence.BoardHeight);
				for (TimeSpan ts = TimeSpan.Zero; ts < Sequence.Length; ts += Sequence.FrameDelay)
				{
					progress.Report(ts.TotalMilliseconds / Sequence.Length.TotalMilliseconds);
					if (cancelToken.IsCancellationRequested) return;
					Sequence.RenderFrameAt(board, ts, true);
					if (cancelToken.IsCancellationRequested) return;
					exporter.AddFrame(board);
				}
				if (cancelToken.IsCancellationRequested) return;
				exporter.FinalizeImage();
			}
			catch (OperationCanceledException) { } // Eat, we are canceling
		}

		public ProjectModel ExportProject()
		{
			return new ProjectModel
			{
				Width = Sequence.BoardWidth,
				Height = Sequence.BoardHeight,
				FrameDelay = (int)Sequence.FrameDelay.TotalMilliseconds,
				Steps = Sequence.Steps.Select(entry => new ProjectStepModel
				{
					Duration = entry.Length.TotalMilliseconds,
					Type = entry.Step.GetType().FullName,
					ConfigurationType = entry.Step.ConfigurationType.FullName,
					Configuration = GetConfigurationMap(entry.Step.CurrentConfiguration),
					Transition = entry.Transition != null ? new ProjectTransitionModel
					{
						Duration = entry.Transition.Length.TotalMilliseconds,
						Type = entry.Transition.GetType().FullName,
						ConfigurationType = entry.Transition.ConfigurationType.FullName,
						Configuration = GetConfigurationMap(entry.Transition.CurrentConfiguration),
					} : null,
				}).ToArray(),
			};
		}

		private ProjectConfigurationMap GetConfigurationMap(object configObj)
		{
			return new ProjectConfigurationMap(configObj.GetType().GetProperties().ToDictionary(prop => prop.Name, prop => prop.GetValue(configObj)));
		}

		#endregion
	}
}
