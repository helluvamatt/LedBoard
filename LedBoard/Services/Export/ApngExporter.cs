using LedBoard.Models;
using LedBoard.Services.Rendering;
using LedBoard.Shared.PNG;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace LedBoard.Services.Export
{
	public class ApngExporter : IExportService
	{
		private readonly BoardRenderer _Renderer;
		private readonly AnimatedPng _Apng;
		private readonly string _FilePath;
		private readonly int _Scale;
		private readonly int _DotPitch;
		private readonly int _PixelSize;
		private readonly byte _MinPixelBrightness;
		private readonly TimeSpan _FrameDelay;

		public ApngExporter(string filePath, int scale, int dotPitch, int pixelSize, byte minPixelBrightess, TimeSpan frameDelay)
		{
			_Renderer = new BoardRenderer();
			_Apng = new AnimatedPng();
			_FilePath = filePath;
			_Scale = scale;
			_DotPitch = dotPitch;
			_PixelSize = pixelSize;
			_MinPixelBrightness = minPixelBrightess;
			_FrameDelay = frameDelay;
		}

		public void AddFrame(IBoard frame)
		{
			// Render frame
			var bitmap = _Renderer.CreateWriteableBitmap(frame.Width, frame.Height, _DotPitch, _PixelSize);
			_Renderer.RenderBoard(frame, bitmap, _DotPitch, _PixelSize, _MinPixelBrightness);

			// Resize if necessary
			BitmapSource resized = bitmap;
			if (_Scale > 1) resized = new TransformedBitmap(resized, new ScaleTransform(_Scale, _Scale));

			// Encode the frame and copy the encoded frame to the stream
			var bmFrame = BitmapFrame.Create(resized);
			var encoder = new PngBitmapEncoder();
			encoder.Frames.Add(bmFrame);
			using (var ms = new MemoryStream())
			{
				encoder.Save(ms);
				ms.Position = 0;
				_Apng.AddFrame(ms, _FrameDelay);
			}
		}

		public void FinalizeImage()
		{
			using (var stream = new FileStream(_FilePath, FileMode.Create, FileAccess.Write))
			{
				_Apng.WriteApngTo(stream);
			}
		}
	}
}
