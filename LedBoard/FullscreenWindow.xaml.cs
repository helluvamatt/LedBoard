using LedBoard.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace LedBoard
{
	/// <summary>
	/// Interaction logic for FullscreenWindow.xaml
	/// </summary>
	public partial class FullscreenWindow : Window
	{
		private readonly int _X;
		private readonly int _Y;
		private readonly int _Width;
		private readonly int _Height;

		public FullscreenWindow(int x, int y, int width, int height)
		{
			_X = x;
			_Y = y;
			_Width = width;
			_Height = height;
			InitializeComponent();
		}

		protected override void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);
			IntPtr hwnd = new WindowInteropHelper(this).Handle;
			User32.MoveWindow(hwnd, _X, _Y, _Width, _Height, false);
			WindowState = WindowState.Maximized;
		}

		protected override void OnKeyUp(KeyEventArgs e)
		{
			if (e.Key == Key.Escape)
			{
				Close();
				e.Handled = true;
			}
			base.OnKeyUp(e);
		}
	}
}
