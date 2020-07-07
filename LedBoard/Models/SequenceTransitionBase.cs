using System;
using System.ComponentModel;

namespace LedBoard.Models
{
	public interface ISequenceTransition : ISequenceItem
	{
		/// <summary>
		/// Called by the sequencer when the step is added to a sequence
		/// </summary>
		/// <param name="width">Board width</param>
		/// <param name="height">Board height</param>
		/// <param name="frameDelay">Frame length</param>
		/// <returns>True if initialization was successful, false otherwise</returns>
		bool Init(int width, int height, TimeSpan frameDelay);

		/// <summary>
		/// Animate a frame and update the given board
		/// </summary>
		/// <param name="target">Board to update</param>
		/// <param name="outgoing">Previous step board</param>
		/// <param name="incoming">Next step board</param>
		/// <param name="frameTime">Time since the beginning of the transition</param>
		void AnimateFrame(IBoard target, IBoard outgoing, IBoard incoming, TimeSpan frameTime);
	}

	public abstract class SequenceTransitionBase<TConfig> : ISequenceTransition where TConfig : class, ICloneable
	{
		private readonly static TimeSpan DEFAULT_LENGTH = TimeSpan.FromSeconds(1);

		private TimeSpan? _Length;
		protected TimeSpan _FrameDelay;

		protected SequenceTransitionBase()
		{
			TypedConfiguration = CreateDefaultConfiguration();
		}

		/// <inheritdoc />
		public Type ConfigurationType => typeof(TConfig);

		/// <inheritdoc />
		public object CurrentConfiguration => TypedConfiguration.Clone();

		/// <summary>
		/// Current strongly-typed configuration object
		/// </summary>
		protected TConfig TypedConfiguration { get; private set; }

		#region Abstract interface

		/// <inheritdoc />
		public abstract string DisplayName { get; }

		/// <inheritdoc />
		public TimeSpan Length
		{
			get => _Length ?? DefaultLength;
			set
			{
				// Enforce a minimum length
				if (value < _FrameDelay) value = _FrameDelay;

				if (_Length != value)
				{
					_Length = value;
					OnPropertyChanged(nameof(Length));
				}
			}
		}

		/// <inheritdoc />
		public TimeSpan DefaultLength => DEFAULT_LENGTH;

		/// <summary>
		/// When overridden in a derived class, creates a default strongly-typed configuration object
		/// </summary>
		/// <returns>TConfig</returns>
		protected abstract TConfig CreateDefaultConfiguration();

		/// <summary>
		/// When overridden in a derived class, performs initialization of the transition, given the board dimensions.
		/// </summary>
		/// <param name="width">Board width</param>
		/// <param name="height">Board height</param>
		/// <returns>True if initialization was successful, false otherwise</returns>
		protected virtual bool OnInit(int width, int height) => true;

		/// <inheritdoc />
		public abstract void AnimateFrame(IBoard target, IBoard outgoing, IBoard incoming, TimeSpan frameTime);

		#endregion

		#region INotifyPropertyChanged impl

		/// <inheritdoc />
		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>
		/// Raise the PropertyChanged event
		/// </summary>
		/// <param name="propertyName">Property name argument</param>
		protected void OnPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		#endregion

		/// <inheritdoc />
		public bool Init(int width, int height, TimeSpan frameDelay)
		{
			_FrameDelay = frameDelay;
			bool result = OnInit(width, height);
			if (result)
			{
				if (!_Length.HasValue) Length = DefaultLength;
				OnPropertyChanged(nameof(Length));
				OnPropertyChanged(nameof(DisplayName));
			}
			return result;
		}

		/// <inheritdoc />
		public void Configure(object newConfiguration)
		{
			TypedConfiguration = newConfiguration as TConfig;
			OnPropertyChanged(nameof(CurrentConfiguration));
		}
	}
}
