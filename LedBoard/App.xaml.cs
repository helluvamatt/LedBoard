using System.Windows;

namespace LedBoard
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
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
