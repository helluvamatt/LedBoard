using System;
using System.ComponentModel;

namespace LedBoard.Models
{
	public interface ISequenceStep : INotifyPropertyChanged
	{
		/// <summary>
		/// Name to display for this step
		/// </summary>
		string DisplayName { get; }

		/// <summary>
		/// Preview board
		/// </summary>
		IBoard Preview { get; }

		/// <summary>
		/// Total length of the animation
		/// </summary>
		TimeSpan Length { get; }

		/// <summary>
		/// Number of frame steps for this step
		/// </summary>
		int StepCount { get; }

		/// <summary>
		/// Type of the configuration object (if present)
		/// </summary>
		Type ConfigurationType { get; }

		/// <summary>
		/// Current configuration representation
		/// </summary>
		object CurrentConfiguration { get; }

		/// <summary>
		/// Called by the sequencer when the step is added to a sequence
		/// </summary>
		/// <param name="width">Board width</param>
		/// <param name="height">Board height</param>
		/// <returns>True if initialization was successful, false otherwise</returns>
		bool Init(int width, int height);

		/// <summary>
		/// (Re)configure the step
		/// </summary>
		/// <param name="newConfiguration"></param>
		void Configure(object newConfiguration);

		/// <summary>
		/// Animate a frame and update the given board
		/// </summary>
		/// <param name="board">Board to update</param>
		/// <param name="step">Frame index local to this step</param>
		/// <param name="afterDelay">Set to the delay to wait until the next frame</param>
		void AnimateFrame(IBoard board, int step, out TimeSpan afterDelay);
	}

	/// <summary>
	/// Represents a step that can be configured. Includes common code for configuration.
	/// </summary>
	/// <typeparam name="TConfig">Type of the configuration object</typeparam>
	public abstract class SequenceStepBase<TConfig> : ISequenceStep where TConfig : class, ICloneable
	{
		protected SequenceStepBase()
		{
			TypedConfiguration = CreateDefaultConfiguration();
		}

		/// <inheritdoc />
		public Type ConfigurationType => typeof(TConfig);

		/// <inheritdoc />
		public object CurrentConfiguration => TypedConfiguration.Clone();

		/// <inheritdoc />
		public IBoard Preview { get; } = new MemoryBoard(16, 16);

		/// <summary>
		/// Current strongly-typed configuration object
		/// </summary>
		protected TConfig TypedConfiguration { get; private set; }

		#region Abstract interface

		/// <inheritdoc />
		public abstract string DisplayName { get; }

		/// <inheritdoc />
		public abstract TimeSpan Length { get; }

		/// <inheritdoc />
		public abstract int StepCount { get; }

		/// <summary>
		/// When overridden in a derived class, creates a default strongly-typed configuration object
		/// </summary>
		/// <returns>TConfig</returns>
		protected abstract TConfig CreateDefaultConfiguration();

		/// <summary>
		/// When overridden in a derived class, performs initialization of the step, given the board dimensions.
		/// </summary>
		/// <param name="width">Board width</param>
		/// <param name="height">Board height</param>
		/// <returns></returns>
		public virtual bool OnInit(int width, int height) => true;

		/// <summary>
		/// When overridden in a derived class, renders the given step offset to the given board, and returns the frame hold delay
		/// </summary>
		/// <param name="board">Render target</param>
		/// <param name="step">Local step offset index</param>
		/// <param name="afterDelay">Frame hold delay</param>
		public abstract void AnimateFrame(IBoard board, int step, out TimeSpan afterDelay);

		/// <summary>
		/// When overridden in a derived class, renders a preview to the target board
		/// </summary>
		/// <param name="previewBoard">Render target</param>
		protected abstract void RenderPreview(IBoard previewBoard);

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
		public bool Init(int width, int height)
		{
			bool result = OnInit(width, height);
			if (result)
			{
				Preview.BeginEdit();
				RenderPreview(Preview);
				Preview.Commit(this);
				OnPropertyChanged(nameof(Preview));
				OnPropertyChanged(nameof(StepCount));
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
