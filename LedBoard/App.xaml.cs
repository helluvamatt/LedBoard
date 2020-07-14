using LedBoard.Services;
using LedBoard.ViewModels;
using System;
using System.IO;
using System.Windows;

namespace LedBoard
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			// Store current app path in registry
			using (var key = RegistrySettingsProvider.OpenAppSettingsKey(true))
			{
				string localAppPath = new Uri(GetType().Assembly.CodeBase).LocalPath;
				key.SetValue(RegistrySettingsProvider.AppPathValueName, localAppPath);
			}

			// Launch main window
			MainWindow = new AppWindow();
			if (e.Args.Length > 0)
			{
				((ShellViewModel)MainWindow.DataContext).LoadProjectOnStartup(e.Args[0]);
			}
			MainWindow.Show();
		}

		protected override void OnSessionEnding(SessionEndingCancelEventArgs e)
		{
			if (MainWindow.DataContext is ICheckDirty checkDirty)
			{
				if (checkDirty.IsDirty)
				{
					e.Cancel = true;
					checkDirty.HandleSessionEnd();
				}
			}
			base.OnSessionEnding(e);
		}
	}
}
