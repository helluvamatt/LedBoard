using LedBoard.Screensaver.Properties;
using LedBoard.Shared.PNG;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace LedBoard.Screensaver
{
	public partial class ScreensaverForm : Form
	{
		private readonly bool _PreviewMode;
		private readonly bool _IsPrimary;

		private Point _InitialPos;
		private AnimatedPng _Apng;

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
			if (_IsPrimary && !string.IsNullOrWhiteSpace(Settings.Default.ImagePath) && File.Exists(Settings.Default.ImagePath))
			{
				try
				{
					bool isPng = false;
					using (var imageFile = new FileStream(Settings.Default.ImagePath, FileMode.Open, FileAccess.Read))
					{
						isPng = AnimatedPng.IsPng(imageFile);
					}

					if (isPng)
					{
						StartPng(Settings.Default.ImagePath);
					}
					else
					{
						pb.Image = Image.FromFile(Settings.Default.ImagePath);
					}
				}
				catch (Exception ex)
				{
					pb.Image = RenderErrorImage($"Failed to load image \"{Settings.Default.ImagePath}\": {ex}");
				}
			}
			base.OnLoad(e);
		}

		protected override void OnFormClosing(FormClosingEventArgs e)
		{
			animationTimer.Stop();
			base.OnFormClosing(e);
		}

		protected override void OnKeyPress(KeyPressEventArgs e)
		{
			if (!_PreviewMode) Application.Exit();
			base.OnKeyPress(e);
		}

		private Image RenderErrorImage(string errorText)
		{
			var font = new Font(FontFamily.GenericMonospace, 10);
			SizeF size;
			using (var measureG = Graphics.FromHwnd(Handle))
			{
				size = measureG.MeasureString(errorText, font);
			}

			var bm = new Bitmap((int)size.Width + 1, (int)size.Height + 1);
			using (var g = Graphics.FromImage(bm))
			{
				g.Clear(Color.Transparent);
				g.DrawString(errorText, font, new SolidBrush(Color.Red), 0, 0);
			}
			return bm;
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

		#region (Animated) PNG

		private readonly List<Bitmap> _Images = new List<Bitmap>();

		private int _CurrentFrameNumber;
		private uint _LoopCount;

		private void StartPng(string filePath)
		{
			_CurrentFrameNumber = 0;
			_LoopCount = 0;
			_Apng = AnimatedPng.FromFile(filePath);
			LoadFrames();
			pb.Image = _Images[_CurrentFrameNumber];
			if (_Apng.HasAnimation)
			{
				animationTimer.Interval = (int)_Apng.GetFrameDelay(0).TotalMilliseconds;
				animationTimer.Start();
			}
		}

		private void LoadFrames()
		{
			// Clear old frames (if needed)
			foreach (var im in _Images) im.Dispose();
			_Images.Clear();

			if (_Apng.HasAnimation)
			{
				Bitmap current = new Bitmap((int)_Apng.Width, (int)_Apng.Height);
				Bitmap previous = null;
				RenderNextFrame(current, Point.Empty, LoadFrame(0), BlendOp.Source);
				_Images.Add(current);

				for (int i = 1; i < _Apng.FrameCount; i++)
				{
					// Handle previous frame
					Bitmap prev = previous == null ? null : new Bitmap(previous);
					DisposeOp dispose = _Apng.GetFrameDispose(i - 1);
					FrameRect prevRect = _Apng.GetFrameRect(i - 1);
					if (dispose == DisposeOp.Previous) previous = new Bitmap(current);
					DisposeBuffer(current, new Rectangle((int)prevRect.X, (int)prevRect.Y, (int)prevRect.Width, (int)prevRect.Height), dispose, prev);

					// Current frame
					FrameRect rect = _Apng.GetFrameRect(i);
					BlendOp blend = _Apng.GetFrameBlend(i);
					RenderNextFrame(current, new Point((int)rect.X, (int)rect.Y), LoadFrame(i), blend);
					_Images.Add(new Bitmap(current));
				}
			}
			else
			{
				using (var stream = new MemoryStream())
				{
					_Apng.WriteDefaultFrameTo(stream);
					stream.Position = 0;
					_Images.Add(new Bitmap(stream));
				}
			}
		}

		private Bitmap LoadFrame(int index)
		{
			using (var stream = new MemoryStream())
			{
				_Apng.WriteFrameTo(stream, index);
				stream.Position = 0;
				return new Bitmap(stream);
			}
		}

		private void NextFrame()
		{
			_CurrentFrameNumber++;
			if (_CurrentFrameNumber >= _Apng.FrameCount)
			{
				_LoopCount++;
				_CurrentFrameNumber = 0;
			}
			pb.Image = _Images[_CurrentFrameNumber];
		}

		private void OnAnimationTimerTick(object sender, EventArgs e)
		{
			if (_Apng.PlayCount == 0 || _LoopCount < _Apng.PlayCount)
			{
				NextFrame();
				animationTimer.Interval = (int)_Apng.GetFrameDelay(_CurrentFrameNumber).TotalMilliseconds;
			}
		}

		private static void DisposeBuffer(Bitmap buffer, Rectangle region, DisposeOp dispose, Bitmap prevBuffer)
		{
			using (Graphics g = Graphics.FromImage(buffer))
			{
				g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;

				Brush b = new SolidBrush(Color.Transparent);
				if (dispose == DisposeOp.Background) g.FillRectangle(b, region);
				else if (dispose == DisposeOp.Previous && prevBuffer != null)
				{
					g.FillRectangle(b, region);
					g.DrawImage(prevBuffer, region, region, GraphicsUnit.Pixel);
				}
			}
		}

		private static void RenderNextFrame(Bitmap buffer, Point point, Bitmap nextFrame, BlendOp blend)
		{
			using (Graphics g = Graphics.FromImage(buffer))
			{
				switch (blend)
				{
					case BlendOp.Over:
						g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
						break;
					case BlendOp.Source:
						g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
						break;
					default:
						break;
				}
				g.DrawImage(nextFrame, point);
			}
		}

		//private static void ClearFrame(Bitmap buffer)
		//{
		//	using (Graphics g = Graphics.FromImage(buffer))
		//	{
		//		g.Clear(Color.Transparent);
		//	}
		//}

		#endregion
	}
}
