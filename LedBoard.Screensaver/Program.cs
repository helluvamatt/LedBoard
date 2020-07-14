using System;
using System.Windows.Forms;

namespace LedBoard.Screensaver
{
	static class Program
	{
		[STAThread]
		static void Main(string[] args)
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new AppContext(args));
		}
	}
}
