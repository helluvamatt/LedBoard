using LedBoard.Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace LedBoard.Models
{
	public interface ISequenceStep : ISequenceItem
	{
		/// <summary>
		/// Preview board
		/// </summary>
		IBoard Preview { get; }

		/// <summary>
		/// Called by the sequencer when the step is added to a sequence
		/// </summary>
		/// <param name="width">Board width</param>
		/// <param name="height">Board height</param>
		/// <param name="frameDelay">Frame length</param>
		/// <param name="resourcesService">Service to load resources from</param>
		/// <returns>True if initialization was successful, false otherwise</returns>
		bool Init(int width, int height, TimeSpan frameDelay, IResourcesService resourcesService);

		/// <summary>
		/// Animate a frame and update the given board
		/// </summary>
		/// <param name="board">Board to update</param>
		/// <param name="localTime">Time index local to this step, including any padding for transitions</param>
		/// <param name="transitionExtraLength">Extra time from transitions that should be added to the length of the step when performing animations</param>
		void AnimateFrame(IBoard board, TimeSpan localTime, TimeSpan transitionExtraLength);

		/// <summary>
		/// Get resource URIs defined by this step
		/// </summary>
		IEnumerable<string> Resources { get; }
	}

	/// <summary>
	/// Represents a step that can be configured. Includes common code for configuration.
	/// </summary>
	/// <typeparam name="TConfig">Type of the configuration object</typeparam>
	public abstract class SequenceStepBase<TConfig> : ISequenceStep where TConfig : class, ICloneable
	{
		private TimeSpan? _Length;
		protected TimeSpan _FrameDelay;

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

		/// <inheritdoc />
		public virtual IEnumerable<string> Resources => Enumerable.Empty<string>();

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
		public abstract TimeSpan DefaultLength { get; }

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
		/// <param name="resourcesService">Resources Service</param>
		/// <returns>True if initialization was successful, false otherwise</returns>
		protected virtual bool OnInit(int width, int height, IResourcesService resourcesService) => true;

		/// <summary>
		/// When overridden in a derived class, renders a frame of animation
		/// </summary>
		/// <param name="board">Render target</param>
		/// <param name="localTime">Time index local to this step, including any padding for transitions</param>
		/// <param name="transitionExtra">Extra transition time to be added to the Length when performing animations</param>
		protected virtual void OnAnimateFrame(IBoard board, TimeSpan localTime, TimeSpan transitionExtra) { }

		/// <summary>
		/// When overridden in a derived class, renders a static frame
		/// </summary>
		/// <param name="board"></param>
		protected virtual void OnAnimateFrame(IBoard board) { }

		/// <summary>
		/// When overridden in a derived class, indicates whether the step type can change the frame based on time
		/// </summary>
		protected virtual bool SupportsAnimation => false;

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
		public bool Init(int width, int height, TimeSpan frameDelay, IResourcesService resourcesService)
		{
			_FrameDelay = frameDelay;
			bool result = OnInit(width, height, resourcesService);
			if (result)
			{
				if (!_Length.HasValue) Length = DefaultLength;
				Preview.BeginEdit();
				RenderPreview(Preview);
				Preview.Commit(this);
				OnPropertyChanged(nameof(Preview));
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

		/// <inheritdoc />
		public void AnimateFrame(IBoard board, TimeSpan localTime, TimeSpan transitionExtra)
		{
			if (SupportsAnimation) OnAnimateFrame(board, localTime, transitionExtra);
			else OnAnimateFrame(board);
		}

		protected int ComputeStep(int availableSteps, TimeSpan frameTime, TimeSpan transitionExtra)
		{
			return (int)(frameTime.TotalMilliseconds / (Length + transitionExtra).TotalMilliseconds * availableSteps);
		}
	}
}
