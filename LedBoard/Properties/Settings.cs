using LedBoard.Interop;
using LedBoard.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.Windows.Threading;

namespace LedBoard.Properties
{
	[SettingsProvider(typeof(RegistrySettingsProvider))]
	internal partial class Settings : ApplicationSettingsBase
	{
		private DispatcherTimer _Timer;

		public Settings()
		{
			MostRecentlyUsedProjects = new ObservableCollection<string>();
			LoadMostRecentlyUsedProjects();
		}

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

		#region MostRecentlyUsed list

		public ObservableCollection<string> MostRecentlyUsedProjects { get; }

		private void LoadMostRecentlyUsedProjects()
		{
			try
			{
				MostRecentlyUsedProjects.Clear();
				foreach (string path in Shell32.GetMRUFiles("*.ledproj"))
				{
					MostRecentlyUsedProjects.Add(path);
				}
			}
			catch
			{
				// Do nothing, something is wrong with the system.
#if DEBUG
				// If this is a DEBUG build, and the debugger is attached, go ahead and throw to the debugger
				if (Debugger.IsAttached) throw;
#endif
			}
		}

		private string _CurrentProject;
		public string CurrentProject
		{
			get => _CurrentProject;
			set
			{
				if (_CurrentProject != value)
				{
					_CurrentProject = value;
					try
					{
						Shell32.AddToRecentlyUsedDocs(_CurrentProject);
						LoadMostRecentlyUsedProjects();
					}
					catch
					{
						// Do nothing, something is wrong with the system.
#if DEBUG
						// If this is a DEBUG build, and the debugger is attached, go ahead and throw to the debugger
						if (Debugger.IsAttached) throw;
#endif
					}
					OnPropertyChanged(this, new PropertyChangedEventArgs(nameof(CurrentProject)));
				}
			}
		}

		#endregion
	}
}
