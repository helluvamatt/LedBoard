using LedBoard.Screensaver.Properties;
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace LedBoard.Screensaver
{
	public partial class ScreensaverForm : Form
	{
		private Point _InitialPos;
		private readonly bool _PreviewMode;
		private readonly bool _IsPrimary;

		public ScreensaverForm(bool isPrimary)
		{
			InitializeComponent();
			_IsPrimary = isPrimary;
		}

		public ScreensaverForm(IntPtr previewHwnd)
		{
			InitializeComponent();

			// Set the preview window as the parent of this window
			Win32Interop.SetParent(Handle, previewHwnd);

			// Make this a child window so it will close when the parent dialog closes
			// GWL_STYLE = -16, WS_CHILD = 0x40000000
			Win32Interop.SetWindowLong(Handle, -16, new IntPtr(Win32Interop.GetWindowLong(Handle, -16) | 0x40000000));

			// Place our window inside the parent
			Win32Interop.GetClientRect(previewHwnd, out Rectangle parentRect);
			Size = parentRect.Size;
			Location = new Point(0, 0);

			_PreviewMode = true;
		}

		#region Form overrides

		protected override void OnLoad(EventArgs e)
		{
			Cursor.Hide();
			if (_IsPrimary && !string.IsNullOrWhiteSpace(Settings.Default.GifPath) && File.Exists(Settings.Default.GifPath))
			{
				try
				{
					var gif = Image.FromFile(Settings.Default.GifPath);
					pb.Image = gif;
				}
				catch { } // Do nothing
			}
			base.OnLoad(e);
		}

		protected override void OnKeyPress(KeyPressEventArgs e)
		{
			if (!_PreviewMode) Application.Exit();
			base.OnKeyPress(e);
		}

		#endregion

		#region Event handlers

		private void OnMouseClick(object sender, MouseEventArgs e)
		{
			if (!_PreviewMode) Application.Exit();
		}

		private void OnMouseMove(object sender, MouseEventArgs e)
		{
			if (!_PreviewMode && !_InitialPos.IsEmpty && (Math.Abs(_InitialPos.X - e.Location.X) > 5 || Math.Abs(_InitialPos.Y - e.Location.Y) > 5))
			{
				Application.Exit();
			}
			_InitialPos = e.Location;
		}

		#endregion
	}
}
