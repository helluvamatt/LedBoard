using LedBoard.Models;
using LedBoard.Services;
using System;
using System.Collections.Generic;
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
		private bool _Loop;

		public SequencerViewModel(IDialogService dialogService, int boardWidth, int boardHeight, int frameDelay)
		{
			_DialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
			CurrentBoard = new MemoryBoard(boardWidth, boardHeight);
			Sequence = new Sequence(Dispatcher, boardWidth, boardHeight, frameDelay);
			Sequence.CurrentFrameChanged += OnSequenceCurrentFrameChanged;
			StopCommand = new DelegateCommand(OnStop);
			PlayPauseCommand = new DelegateCommand(OnPlayPause);
			StepForwardCommand = new DelegateCommand(OnStepForward, () => !IsPlaying);
			StepBackwardCommand = new DelegateCommand(OnStepBackward, () => !IsPlaying);
			AddItemCommand = new DelegateCommand(OnAddItem, () => !IsPlaying && SelectedStepType != null);
			DeleteItemCommand = new DelegateCommand(OnDeleteSelectedItem, () => !IsPlaying && SelectedItem != null);
		}

		public IBoard CurrentBoard { get; }
		public Sequence Sequence { get; }

		public IEnumerable<StepDescriptor> StepTypes => StepService.StepTypes;

		#region Dependency properties

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

		#region IsLooping

		public static readonly DependencyProperty IsLoopingProperty = DependencyProperty.Register(nameof(IsLooping), typeof(bool), typeof(SequencerViewModel), new PropertyMetadata(false, OnIsLoopingChanged));

		public bool IsLooping
		{
			get => (bool)GetValue(IsLoopingProperty);
			set => SetValue(IsLoopingProperty, value);
		}

		private static void OnIsLoopingChanged(DependencyObject owner, DependencyPropertyChangedEventArgs e)
		{
			var vm = (SequencerViewModel)owner;
			vm._Loop = vm.IsLooping;
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

		public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register(nameof(SelectedItem), typeof(SequenceEntry), typeof(SequencerViewModel), new PropertyMetadata(null, OnSelectedItemChanged));

		public SequenceEntry SelectedItem
		{
			get => (SequenceEntry)GetValue(SelectedItemProperty);
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
			if (SelectedItem != null) index = Sequence.Steps.IndexOf(SelectedItem);
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

			_DialogService.ConfirmDialog("Are you sure you want to delete this item?", SelectedItem.Step.DisplayName, result =>
			{
				if (result)
				{
					Sequence.Steps.Remove(SelectedItem);
					SelectedItem = null;
				}
			});
		}

		#endregion

		#endregion

		private void OnSequenceCurrentFrameChanged(object sender, EventArgs e)
		{
			Sequence.GetCurrentFrame(CurrentBoard);
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
			while (_CancelController != null && !_CancelController.IsCancellationRequested)
			{
				hasMore = Sequence.Advance(CurrentBoard);
				if (hasMore || _Loop)
				{
					try
					{
						await Task.Delay(Sequence.FrameDelay, _CancelController.Token);
					}
					catch (OperationCanceledException) { } // Eat, we are pausing
					if (!hasMore && _Loop) Sequence.CurrentTime = 0;
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
				var board = new MemoryBoard(CurrentBoard.Width, CurrentBoard.Height);
				for (TimeSpan ts = TimeSpan.Zero; ts < Sequence.Length; ts += Sequence.FrameDelay)
				{
					progress.Report(ts.TotalMilliseconds / Sequence.Length.TotalMilliseconds);
					if (cancelToken.IsCancellationRequested) return;
					Sequence.RenderFrameAt(board, ts);
					if (cancelToken.IsCancellationRequested) return;
					exporter.AddFrame(board);
				}
				if (cancelToken.IsCancellationRequested) return;
				exporter.FinalizeImage();
			}
			catch (OperationCanceledException) { } // Eat, we are canceling
		}

		#endregion
	}
}
