using LedBoard.ViewModels;
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
			MainWindow = new MasterWindow();
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
