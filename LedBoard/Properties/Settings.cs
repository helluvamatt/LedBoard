using LedBoard.Services;
using System;
using System.ComponentModel;
using System.Configuration;
using System.Windows.Threading;

namespace LedBoard.Properties
{
	[SettingsProvider(typeof(RegistrySettingsProvider))]
	internal partial class Settings : ApplicationSettingsBase
	{
		private DispatcherTimer _Timer;

		protected override void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(sender, e);

			// Debounce
			_Timer?.Stop();
			_Timer = new DispatcherTimer(TimeSpan.FromMilliseconds(250), DispatcherPriority.ApplicationIdle, OnTimerTick, Dispatcher.CurrentDispatcher);
			_Timer.Start();
		}

		private void OnTimerTick(object sender, EventArgs e)
		{
			if (_Timer == null) return;
			_Timer.Stop();
			_Timer = null;
			Save();
		}
	}
}
