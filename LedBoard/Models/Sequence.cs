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
		private readonly int _BoardWidth;
		private readonly int _BoardHeight;

		private TimeSpan _CurrentTime;
		private SequenceEntry _CurrentEntry;

		public Sequence(Dispatcher dispatcher, int boardWidth, int boardHeight, int frameDelay)
		{
			_Dispatcher = dispatcher;
			_BoardWidth = boardWidth;
			_BoardHeight = boardHeight;
			FrameDelay = TimeSpan.FromMilliseconds(frameDelay);
			Steps = new ObservableCollection<SequenceEntry>();
			Steps.CollectionChanged += OnStepsCollectionChanged;
		}

		public TimeSpan FrameDelay { get; }
		public TimeSpan Length => TimeSpan.FromMilliseconds(Steps.Sum(step => step.Length.TotalMilliseconds));

		public ObservableCollection<SequenceEntry> Steps { get; }

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
			if (lookupStep) _CurrentEntry = FindAtTime(_CurrentTime);
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentTime)));
			CurrentFrameChanged?.Invoke(this, EventArgs.Empty);
		}

		private SequenceEntry FindAtTime(TimeSpan time)
		{
			// Shortcut for the first step
			if (time == TimeSpan.Zero) return Steps.FirstOrDefault();

			foreach (SequenceEntry current in Steps.Reverse())
			{
				if (time >= current.StartTime) return current;
			}

			return Steps.FirstOrDefault();
		}

		#endregion

		public bool Advance(IBoard board)
		{
			board.BeginEdit();
			_CurrentEntry.Step.AnimateFrame(board, GetCurrentStepOffset(out TimeSpan stepOffsetTime));
			board.Commit(this);
			SetCurrentTime(_CurrentTime + FrameDelay, stepOffsetTime + FrameDelay >= _CurrentEntry.Length);
			return _CurrentTime < Length;
		}

		public void GetCurrentFrame(IBoard board)
		{
			board.BeginEdit();
			_CurrentEntry.Step.AnimateFrame(board, GetCurrentStepOffset(out _));
			board.Commit(this);
		}

		public void RenderFrameAt(IBoard board, TimeSpan ts)
		{
			var entry = FindAtTime(ts);
			if (entry != null)
			{
				board.BeginEdit();
				entry.Step.AnimateFrame(board, GetStepOffset(entry, ts, out _));
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
				}
			}

			// Initialize new steps
			if (e.Action == NotifyCollectionChangedAction.Add || e.Action == NotifyCollectionChangedAction.Replace)
			{
				foreach (SequenceEntry entry in e.NewItems)
				{
					entry.Init(_Dispatcher, _BoardWidth, _BoardHeight, FrameDelay);
					entry.PropertyChanged += OnSequenceEntryPropertyChanged;
				}
			}

			RecomputeTimeline();
		}

		private void OnSequenceEntryPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			var entry = (SequenceEntry)sender;
			if (e.PropertyName == nameof(ISequenceStep.CurrentConfiguration))
			{
				// Reinitialize on configuration changes
				entry.Init(_Dispatcher, _BoardWidth, _BoardHeight, FrameDelay);

				// Tell the sequencer to update the current frame
				CurrentFrameChanged?.Invoke(this, EventArgs.Empty);
			}
			else if (e.PropertyName == nameof(ISequenceStep.Length))
			{
				RecomputeTimeline();
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

		private int GetCurrentStepOffset(out TimeSpan stepOffsetTime)
		{
			return GetStepOffset(_CurrentEntry, _CurrentTime, out stepOffsetTime);
		}

		private int GetStepOffset(SequenceEntry entry, TimeSpan ts, out TimeSpan stepOffsetTime)
		{
			stepOffsetTime = ts - entry.StartTime;
			return (int)(stepOffsetTime.TotalMilliseconds / FrameDelay.TotalMilliseconds);
		}
	}

	public class SequenceEntry : INotifyPropertyChanged
	{
		public SequenceEntry(ISequenceStep step)
		{
			Step = step ?? throw new ArgumentNullException(nameof(step));
			Step.PropertyChanged += (sender, e) => PropertyChanged?.Invoke(this, e);
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

		public ISequenceStep Step { get; }

		public TimeSpan Length => Step.Length;

		public void Init(Dispatcher dispatcher, int boardWidth, int boardHeight, TimeSpan frameDelay)
		{
			Step.Init(boardWidth, boardHeight, frameDelay);
			dispatcher.Invoke(() =>
			{
				IsReady = true;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Length)));
			});
		}

		public event PropertyChangedEventHandler PropertyChanged;

		public override bool Equals(object obj) => obj is SequenceEntry entry && entry.Step == Step;
		public override int GetHashCode() => Step.GetHashCode();
		public override string ToString() => Step.ToString();
	}
}
