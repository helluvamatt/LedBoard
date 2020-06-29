using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Security.Policy;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace LedBoard.Models
{
	public class Sequence : INotifyPropertyChanged
	{
		private readonly Dispatcher _Dispatcher;
		private readonly int _BoardWidth;
		private readonly int _BoardHeight;

		private int _CurrentStepIndex;
		private SequenceEntry _CurrentEntry;

		public Sequence(Dispatcher dispatcher, int boardWidth, int boardHeight)
		{
			_Dispatcher = dispatcher;
			_BoardWidth = boardWidth;
			_BoardHeight = boardHeight;
			Steps = new ObservableCollection<SequenceEntry>();
			Steps.CollectionChanged += OnStepsCollectionChanged;
		}

		public TimeSpan Length => TimeSpan.FromMilliseconds(Steps.Sum(step => step.Length.TotalMilliseconds));
		public int StepCount => Steps.Sum(step => step.StepCount);
		public int Count => Steps.Count;

		public ObservableCollection<SequenceEntry> Steps { get; }

		#region CurrentStep property

		public int CurrentStep
		{
			get => _CurrentStepIndex;
			set
			{
				if (_CurrentStepIndex != value)
				{
					SetCurrentStep(value, true);
					CurrentFrameChanged?.Invoke(this, EventArgs.Empty);
				}
			}
		}

		private void SetCurrentStep(int value, bool lookupStep)
		{
			_CurrentStepIndex = value;
			if (lookupStep) _CurrentEntry = FindAtStep(_CurrentStepIndex);
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentStep)));
			//PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentTime)));
		}

		private SequenceEntry FindAtStep(int step)
		{
			// Shortcut for the first step
			if (step == 0) return Steps.FirstOrDefault();

			foreach (SequenceEntry current in Steps.Reverse())
			{
				if (step >= current.Start) return current;
			}

			return Steps.FirstOrDefault();
		}

		#endregion

		//#region CurrentTime property

		//private TimeSpan _CurrentTime;
		//public double CurrentTime
		//{
		//	get => _CurrentTime.TotalMilliseconds;
		//	set
		//	{
		//		if (_CurrentTime.TotalMilliseconds != value)
		//		{
		//			SetCurrentTime(TimeSpan.FromMilliseconds(value), true);
		//			CurrentFrameChanged?.Invoke(this, EventArgs.Empty);
		//		}
		//	}
		//}

		//private void SetCurrentTime(TimeSpan value, bool lookupStep)
		//{
		//	_CurrentTime = value;
		//	if (lookupStep) _CurrentEntry = FindAtTime(_CurrentTime);
		//	PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentStep)));
		//	PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentTime)));
		//}

		//private SequenceEntry FindAtTime(TimeSpan time)
		//{
		//	// Shortcut for the first step
		//	if (time == TimeSpan.Zero) return Steps.FirstOrDefault();

		//	foreach (SequenceEntry current in Steps.Reverse())
		//	{
		//		if (time >= current.StartTime) return current;
		//	}

		//	return Steps.FirstOrDefault();
		//}

		//#endregion

		public bool Advance(IBoard board, out TimeSpan nextDelay)
		{
			int stepOffset = CurrentStep - _CurrentEntry.Start;
			board.BeginEdit();
			_CurrentEntry.Step.AnimateFrame(board, stepOffset, out nextDelay);
			board.Commit(this);
			SetCurrentStep(CurrentStep + 1, stepOffset + 1 >= _CurrentEntry.StepCount);
			return CurrentStep < StepCount;
		}

		public void GetCurrentFrame(IBoard board)
		{
			board.BeginEdit();
			_CurrentEntry.Step.AnimateFrame(board, CurrentStep - _CurrentEntry.Start, out _);
			board.Commit(this);
		}

		#region Events

		public event PropertyChangedEventHandler PropertyChanged;
		public event EventHandler CurrentFrameChanged;

		private void OnStepsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (_CurrentEntry == null) SetCurrentStep(CurrentStep, true);

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
					entry.Init(_Dispatcher, _BoardWidth, _BoardHeight);
					entry.PropertyChanged += OnSequenceEntryPropertyChanged;
				}
			}

			RecomputeTimeline();
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));
		}

		private void OnSequenceEntryPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			var entry = (SequenceEntry)sender;
			if (e.PropertyName == nameof(ISequenceStep.CurrentConfiguration))
			{
				// Reinitialize on configuration changes
				entry.Init(_Dispatcher, _BoardWidth, _BoardHeight);

				// Tell the sequencer to update the current frame
				CurrentFrameChanged?.Invoke(this, EventArgs.Empty);
			}
			else if (e.PropertyName == nameof(ISequenceStep.StepCount))
			{
				RecomputeTimeline();
			}
		}

		#endregion

		private void RecomputeTimeline()
		{
			int currentStart = 0;
			TimeSpan currentStartTime = TimeSpan.Zero;
			foreach (SequenceEntry entry in Steps)
			{
				entry.Start = currentStart;
				entry.StartTime = currentStartTime;
				currentStart += entry.StepCount;
				currentStartTime += entry.Length;
			}

			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Length)));
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StepCount)));
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

		private int _Start = 0;
		public int Start
		{
			get => _Start;
			set
			{
				if (_Start != value)
				{
					_Start = value;
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Start)));
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
		public int StepCount => Step.StepCount;
		public TimeSpan Length => Step.Length;

		public void Init(Dispatcher dispatcher, int boardWidth, int boardHeight)
		{
			Step.Init(boardWidth, boardHeight);
			dispatcher.Invoke(() =>
			{
				IsReady = true;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StepCount)));
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Length)));
			});
		}

		public event PropertyChangedEventHandler PropertyChanged;

		public override bool Equals(object obj) => obj is SequenceEntry entry && entry.Step == Step;
		public override int GetHashCode() => Step.GetHashCode();
		public override string ToString() => Step.ToString();
	}
}
