using LedBoard.Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

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
		TimeSpan Length { get; set; }

		/// <summary>
		/// Get the default duration of the animation
		/// </summary>
		TimeSpan DefaultLength { get; }

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
		/// <param name="frameDelay">Frame length</param>
		/// <param name="resourcesService">Service to load resources from</param>
		/// <returns>True if initialization was successful, false otherwise</returns>
		bool Init(int width, int height, TimeSpan frameDelay, IResourcesService resourcesService);

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
		void AnimateFrame(IBoard board, int step);

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
		/// <param name="frameDelay">Frame length</param>
		/// <param name="resourcesService">Resources Service</param>
		/// <returns>True if initialization was successful, false otherwise</returns>
		protected virtual bool OnInit(int width, int height, TimeSpan frameDelay, IResourcesService resourcesService) => true;

		/// <inheritdoc />
		public abstract void AnimateFrame(IBoard board, int step);

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
			if (!_Length.HasValue) Length = DefaultLength;
			_FrameDelay = frameDelay;
			bool result = OnInit(width, height, frameDelay, resourcesService);
			if (result)
			{
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

		protected int ComputeVariableLengthStep(int step, int availableSteps)
		{
			double timeOffset = step * _FrameDelay.TotalMilliseconds;
			double animStep = timeOffset / Length.TotalMilliseconds;
			return (int)(animStep * availableSteps);
		}
	}
}
