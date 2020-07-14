using Mono.Options;
using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace LedBoard.Screensaver
{
	internal class AppContext : ApplicationContext
	{
		public AppContext(string[] args)
		{
			// Process command-line arguments
			LaunchMode launch = LaunchMode.Screensaver;
			IntPtr previewHwnd = IntPtr.Zero;
			var optionSet = new OptionSet()
			{
				{ "s", "Launch screensaver", s => launch = LaunchMode.Screensaver },
				{ "c", "Configure screensaver", c => launch = LaunchMode.Config },
				{ "p=", "Preview mode", p => { previewHwnd = new IntPtr(long.Parse(p)); launch = LaunchMode.Preview; } }
			};
			try
			{
				optionSet.Parse(args);
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Failed to parse command line arguments: {ex}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}

			switch (launch)
			{
				case LaunchMode.Preview:
					MainForm = new ScreensaverForm(previewHwnd);
					MainForm.Show();
					break;
				case LaunchMode.Screensaver:
					ShowScreensaver();
					break;
				case LaunchMode.Config:
				default:
					MainForm = new ConfigForm();
					MainForm.Show();
					break;
			}
		}

		private void ShowScreensaver()
		{
			foreach (Screen screen in Screen.AllScreens)
			{
				var form = new ScreensaverForm(screen.Primary);
				form.Show();
#if DEBUG
				// If we debugging, reset TopMost so we can Alt+Tab back to Visual Studio if needed
				if (Debugger.IsAttached) form.TopMost = false;
#endif
				Win32Interop.MoveWindow(form.Handle, screen.Bounds.X, screen.Bounds.Y, screen.Bounds.Width, screen.Bounds.Height, false);
			}
		}

		private enum LaunchMode { Config, Preview, Screensaver }
	}
}
