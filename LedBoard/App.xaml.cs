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
			MainWindow = new MainWindow();
			if (e.Args.Length > 0)
			{
				((MainWindow)MainWindow).LoadProjectOnStartup(e.Args[0]);
			}
			MainWindow.Show();
		}

		protected override void OnSessionEnding(SessionEndingCancelEventArgs e)
		{
			if (MainWindow is ICheckDirty checkDirty)
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
