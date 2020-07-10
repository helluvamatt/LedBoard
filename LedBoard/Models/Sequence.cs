using LedBoard.Services;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows.Threading;

namespace LedBoard.Models
{
	public class Sequence : INotifyPropertyChanged
	{
		private readonly Dispatcher _Dispatcher;
		private readonly IResourcesService _ResourcesService;
		
		private IBoard _PrevBoard;
		private IBoard _NextBoard;

		private bool _IsDirty;
		private TimeSpan _CurrentTime;
		private SequenceEntry _CurrentEntry;
		private int _CurrentEntryIndex;

		public Sequence(Dispatcher dispatcher, IResourcesService resourcesService)
		{
			_Dispatcher = dispatcher;
			_ResourcesService = resourcesService;
			Steps = new ObservableCollection<SequenceEntry>();
			Steps.CollectionChanged += OnStepsCollectionChanged;
		}

		public void Configure(int boardWidth, int boardHeight, int frameDelay)
		{
			BoardWidth = boardWidth;
			BoardHeight = boardHeight;
			FrameDelay = TimeSpan.FromMilliseconds(frameDelay);
			_PrevBoard = new MemoryBoard(BoardWidth, BoardHeight);
			_NextBoard = new MemoryBoard(BoardWidth, BoardHeight);
			foreach (var entry in Steps)
			{
				// (Re)init step and transition
				entry.InitStep(_Dispatcher, BoardWidth, BoardHeight, FrameDelay, _ResourcesService);
			}
		}

		public int BoardWidth { get; private set; }
		public int BoardHeight { get; private set; }
		public TimeSpan FrameDelay { get; private set; }

		public TimeSpan Length => TimeSpan.FromMilliseconds(Steps.Sum(step => step.Length.TotalMilliseconds));

		#region IsDirty property

		public bool IsDirty
		{
			get => _IsDirty;
			set
			{
				if (_IsDirty != value)
				{
					_IsDirty = value;
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsDirty)));
				}
			}
		}

		public void ResetDirty()
		{
			IsDirty = false;
		}

		#endregion

		public ObservableCollection<SequenceEntry> Steps { get; }

		#region Loop property

		private bool _Loop;
		public bool Loop
		{
			get => _Loop;
			set
			{
				if (_Loop != value)
				{
					_Loop = value;
					CurrentFrameChanged?.Invoke(this, EventArgs.Empty);
				}
			}
		}

		#endregion

		#region CurrentTime property

		public double CurrentTime
		{
			get => _CurrentTime.TotalMilliseconds;
			set
			{
				if (_CurrentTime.TotalMilliseconds != value)
				{
					SetCurrentTime(TimeSpan.FromMilliseconds(value), true);
				}
			}
		}

		private void SetCurrentTime(TimeSpan value, bool lookupStep)
		{
			_CurrentTime = value;
			if (lookupStep) _CurrentEntry = FindAtTime(_CurrentTime, out _CurrentEntryIndex);
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentTime)));
			CurrentFrameChanged?.Invoke(this, EventArgs.Empty);
		}

		private SequenceEntry FindAtTime(TimeSpan time, out int index)
		{
			// Shortcut for the first step
			if (time == TimeSpan.Zero)
			{
				index = 0;
				return Steps.FirstOrDefault();
			}

			for (int i = Steps.Count - 1; i >= 0; i--)
			{
				var current = Steps[i];
				if (time >= current.StartTime)
				{
					index = i;
					return current;
				}
			}

			index = 0;
			return Steps.FirstOrDefault();
		}

		#endregion

		public bool Advance(IBoard board)
		{
			TimeSpan stepOffsetTime = _CurrentTime - _CurrentEntry.StartTime;
			Render(board, _CurrentEntry, _CurrentEntryIndex, stepOffsetTime, Loop);
			SetCurrentTime(_CurrentTime + FrameDelay, stepOffsetTime + FrameDelay >= _CurrentEntry.Length);
			return _CurrentTime < Length;
		}

		public void GetCurrentFrame(IBoard board)
		{
			Render(board, _CurrentEntry, _CurrentEntryIndex, _CurrentTime - _CurrentEntry.StartTime, Loop);
		}

		public void RenderFrameAt(IBoard board, TimeSpan ts, bool loop)
		{
			var entry = FindAtTime(ts, out int index);
			if (entry != null)
			{
				Render(board, entry, index, ts - entry.StartTime, loop);
			}
		}

		private void Render(IBoard board, SequenceEntry entry, int entryIndex, TimeSpan tsOffset, bool loop)
		{
			SequenceEntry previousEntry = null;
			SequenceEntry nextEntry = null;
			if (entryIndex > 0 || loop)
			{
				int prevIndex = entryIndex > 0 ? entryIndex - 1 : Steps.Count - 1;
				previousEntry = Steps[prevIndex];
			}
			if (entryIndex < Steps.Count - 1 || loop)
			{
				int nextIndex = entryIndex < Steps.Count - 1 ? entryIndex + 1 : 0;
				nextEntry = Steps[nextIndex];
			}

			if (tsOffset < entry.StartTransitionLength && previousEntry?.Transition != null)
			{
				// Time since the start of the transition (includes the previous entry's transition length)
				TimeSpan nextOffset = tsOffset + previousEntry.EndTransitionLength;

				// We are in the transition from the previous step
				_PrevBoard.BeginEdit();
				previousEntry.Step.AnimateFrame(_PrevBoard, previousEntry.StartTransitionLength + previousEntry.Length + tsOffset, previousEntry.StartTransitionLength + previousEntry.EndTransitionLength);
				_PrevBoard.Commit(this);

				_NextBoard.BeginEdit();
				entry.Step.AnimateFrame(_NextBoard, nextOffset, entry.StartTransitionLength + entry.EndTransitionLength);
				_NextBoard.Commit(this);

				board.BeginEdit();
				previousEntry.Transition.AnimateFrame(board, _PrevBoard, _NextBoard, nextOffset);
				board.Commit(this);
			}
			else if (tsOffset > entry.Length - entry.EndTransitionLength && entry.Transition != null && nextEntry != null)
			{
				// Time since the start of the transition
				// Works by taking the current time from the beginning of this step, adding the end transition length (extending past the end of the step), then subtracting the length of the step
				TimeSpan nextOffset = tsOffset + entry.EndTransitionLength - entry.Length;

				_PrevBoard.BeginEdit();
				entry.Step.AnimateFrame(_PrevBoard, tsOffset + entry.StartTransitionLength, entry.StartTransitionLength + entry.EndTransitionLength);
				_PrevBoard.Commit(this);

				_NextBoard.BeginEdit();
				nextEntry.Step.AnimateFrame(_NextBoard, nextOffset, nextEntry.StartTransitionLength + nextEntry.EndTransitionLength);
				_NextBoard.Commit(this);

				board.BeginEdit();
				entry.Transition.AnimateFrame(board, _PrevBoard, _NextBoard, nextOffset);
				board.Commit(this);
			}
			else
			{
				// If we have a previous entry (either by looping or by being the 2nd or beyond step) add it's transition padding to our frame time
				TimeSpan extra = TimeSpan.Zero;
				if (nextEntry != null)
				{
					extra += entry.EndTransitionLength;
				}
				if (previousEntry != null)
				{
					tsOffset += previousEntry.EndTransitionLength;
					extra += entry.StartTransitionLength;
				}

				// We are not in a transition, just render the board directly
				board.BeginEdit();
				entry.Step.AnimateFrame(board, tsOffset, extra);
				board.Commit(this);
			}
		}

		public void StepForward()
		{
			TimeSpan newTime = _CurrentTime + FrameDelay;
			if (newTime < Length) SetCurrentTime(newTime, true);
		}

		public void StepBackward()
		{
			TimeSpan newTime = _CurrentTime - FrameDelay;
			if (newTime < TimeSpan.Zero) newTime = TimeSpan.Zero;
			SetCurrentTime(newTime, true);
		}

		#region Events

		public event PropertyChangedEventHandler PropertyChanged;
		public event EventHandler CurrentFrameChanged;

		private void OnStepsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (_CurrentEntry == null) SetCurrentTime(_CurrentTime, true);

			if (e.Action == NotifyCollectionChangedAction.Remove || e.Action == NotifyCollectionChangedAction.Replace)
			{
				foreach (SequenceEntry entry in e.OldItems)
				{
					entry.PropertyChanged -= OnSequenceEntryPropertyChanged;
					entry.StepConfigurationChanged -= OnSequenceEntryStepConfigurationChanged;
					entry.TransitionConfigurationChanged -= OnSequenceEntryTransitionConfigurationChanged;
				}
			}

			// Initialize new steps
			if (e.Action == NotifyCollectionChangedAction.Add || e.Action == NotifyCollectionChangedAction.Replace)
			{
				foreach (SequenceEntry entry in e.NewItems)
				{
					entry.InitStep(_Dispatcher, BoardWidth, BoardHeight, FrameDelay, _ResourcesService);
					entry.PropertyChanged += OnSequenceEntryPropertyChanged;
					entry.StepConfigurationChanged += OnSequenceEntryStepConfigurationChanged;
					entry.TransitionConfigurationChanged += OnSequenceEntryTransitionConfigurationChanged;
					RecomputeTransitionExtra(entry);
				}
			}

			IsDirty = true;

			RecomputeTimeline();
		}

		private void OnSequenceEntryStepConfigurationChanged(object sender, EventArgs e)
		{
			var entry = (SequenceEntry)sender;

			// Reinitialize on configuration changes
			entry.InitStep(_Dispatcher, BoardWidth, BoardHeight, FrameDelay, _ResourcesService);

			// Tell the sequencer to update the current frame
			CurrentFrameChanged?.Invoke(this, EventArgs.Empty);

			IsDirty = true;
		}

		private void OnSequenceEntryTransitionConfigurationChanged(object sender, EventArgs e)
		{
			var entry = (SequenceEntry)sender;
			OnTransitionChanged(entry);
		}

		private void OnSequenceEntryPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			var entry = (SequenceEntry)sender;
			if (e.PropertyName == nameof(SequenceEntry.Length))
			{
				IsDirty = true;
				RecomputeTimeline();
			}
			else if (e.PropertyName == nameof(SequenceEntry.Transition))
			{
				OnTransitionChanged(entry);
			}
		}

		#endregion

		private void RecomputeTimeline()
		{
			TimeSpan currentStartTime = TimeSpan.Zero;
			foreach (SequenceEntry entry in Steps)
			{
				entry.StartTime = currentStartTime;
				currentStartTime += entry.Length;
			}

			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Length)));
		}

		private void OnTransitionChanged(SequenceEntry entry)
		{
			// Initialize the transition
			entry.InitTransition(BoardWidth, BoardHeight, FrameDelay);

			// Recompute TransitionExtraLength for this entry, the one before, and the one after, looping around as necessary
			RecomputeTransitionExtra(entry);

			// Tell the sequencer to update the current frame, the transition may affect the current board
			CurrentFrameChanged?.Invoke(this, EventArgs.Empty);

			IsDirty = true;
		}

		private void RecomputeTransitionExtra(SequenceEntry entry)
		{
			// Compute previous and next indexes, looping as necessary
			int index = Steps.IndexOf(entry);
			int prevIndex = index > 0 ? index - 1 : Steps.Count - 1;
			int nextIndex = index < Steps.Count - 1 ? index + 1 : 0;

			ComputeTransitionExtra(entry, index);
			if (prevIndex != index)
			{
				SequenceEntry prev = Steps[prevIndex];
				ComputeTransitionExtra(prev, prevIndex);
			}
			if (nextIndex != prevIndex)
			{
				SequenceEntry next = Steps[nextIndex];
				ComputeTransitionExtra(next, nextIndex);
			}
		}

		private void ComputeTransitionExtra(SequenceEntry entry, int index)
		{
			int prevIndex = index > 0 ? index - 1 : Steps.Count - 1;
			ISequenceTransition prevTransition = Steps[prevIndex].Transition;

			if (prevTransition != null) entry.StartTransitionLength = TimeSpan.FromMilliseconds(prevTransition.Length.TotalMilliseconds / 2);
			else entry.StartTransitionLength = TimeSpan.Zero;
			if (entry.Transition != null) entry.EndTransitionLength = TimeSpan.FromMilliseconds(entry.Transition.Length.TotalMilliseconds / 2);
			else entry.EndTransitionLength = TimeSpan.Zero;
		}
	}

	public class SequenceEntry : INotifyPropertyChanged
	{
		private bool _IsInitTransition = false;

		public SequenceEntry(ISequenceStep step)
		{
			Step = step ?? throw new ArgumentNullException(nameof(step));
			Step.PropertyChanged += OnStepPropertyChanged;
			StartTransitionLength = TimeSpan.Zero;
			EndTransitionLength = TimeSpan.Zero;
		}

		private bool _IsReady = false;
		public bool IsReady
		{
			get => _IsReady;
			set
			{
				if (_IsReady != value)
				{
					_IsReady = value;
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsReady)));
				}
			}
		}

		private TimeSpan _StartTime;
		public TimeSpan StartTime
		{
			get => _StartTime;
			set
			{
				if (_StartTime != value)
				{
					_StartTime = value;
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StartTime)));
				}
			}
		}

		private ISequenceTransition _Transition;
		public ISequenceTransition Transition
		{
			get => _Transition;
			set
			{
				if (_Transition != value)
				{
					if (_Transition != null) _Transition.PropertyChanged -= OnTransitionPropertyChanged;
					_Transition = value;
					if (_Transition != null) _Transition.PropertyChanged += OnTransitionPropertyChanged;
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Transition)));
				}
			}
		}

		public ISequenceStep Step { get; }

		public TimeSpan Length => Step.Length;
		public TimeSpan StartTransitionLength { get; set; }
		public TimeSpan EndTransitionLength { get; set; }

		public void InitStep(Dispatcher dispatcher, int boardWidth, int boardHeight, TimeSpan frameDelay, IResourcesService resourcesService)
		{
			Step.Init(boardWidth, boardHeight, frameDelay, resourcesService);
			InitTransition(boardWidth, boardHeight, frameDelay);
			dispatcher.Invoke(() =>
			{
				IsReady = true;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Length)));
			});
		}

		public void InitTransition(int boardWidth, int boardHeight, TimeSpan frameDelay)
		{
			// Check re-entrancy
			if (_IsInitTransition) return;
			_IsInitTransition = true;
			Transition?.Init(boardWidth, boardHeight, frameDelay);
			_IsInitTransition = false;
		}

		public bool HandleResize(double deltaMs)
		{
			var newLength = TimeSpan.FromMilliseconds(Step.Length.TotalMilliseconds + deltaMs);
			Step.Length = newLength;
			return Step.Length == newLength;
		}

		public event PropertyChangedEventHandler PropertyChanged;
		public event EventHandler StepConfigurationChanged;
		public event EventHandler TransitionConfigurationChanged;

		private void OnStepPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(ISequenceStep.Length))
			{
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Length)));
			}
			else if (e.PropertyName == nameof(ISequenceStep.CurrentConfiguration))
			{
				StepConfigurationChanged?.Invoke(this, EventArgs.Empty);
			}
			else
			{
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Step)));
			}
		}

		private void OnTransitionPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(ISequenceTransition.CurrentConfiguration))
			{
				TransitionConfigurationChanged?.Invoke(this, EventArgs.Empty);
			}
			else
			{
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Transition)));
			}
		}

		public override bool Equals(object obj) => obj is SequenceEntry entry && entry.Step == Step;
		public override int GetHashCode() => Step.GetHashCode();
		public override string ToString() => Step.ToString();
	}
}
