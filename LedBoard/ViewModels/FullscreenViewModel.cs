using System;
using System.Windows;
using System.Windows.Threading;

namespace LedBoard.ViewModels
{
	public class FullscreenViewModel : DependencyObject
	{
		private readonly DispatcherTimer _Timer;

		public FullscreenViewModel(SequencerViewModel sequencer)
		{
			Sequencer = sequencer;
			MessageShown = true;
			_Timer = new DispatcherTimer(TimeSpan.FromSeconds(5), DispatcherPriority.Normal, OnTimerTick, Dispatcher);
		}

		public SequencerViewModel Sequencer { get; }

		public static readonly DependencyProperty MessageShownProperty = DependencyProperty.Register(nameof(MessageShown), typeof(bool), typeof(FullscreenViewModel), new PropertyMetadata(true));

		public bool MessageShown
		{
			get => (bool)GetValue(MessageShownProperty);
			set => SetValue(MessageShownProperty, value);
		}

		private void OnTimerTick(object sender, EventArgs e)
		{
			_Timer.Stop();
			MessageShown = false;
		}
	}
}
