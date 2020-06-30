using LedBoard.Models;
using LedBoard.Services.Rendering;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace LedBoard.Services.Export
{
	public class GifExporter : IExportService
	{
		private static readonly byte[] APPEXT = new byte[]
		{
			0x21, 0xFF, // Application Extension block
			11, // 11 bytes follow
			(byte)'N', (byte)'E', (byte)'T', (byte)'S', (byte)'C', (byte)'A', (byte)'P', (byte)'E', // 8 character application name
			(byte)'2', (byte)'.', (byte)'0', // application "authentication code"
			3, // 3 more bytes of data
			1, // Data sub-block index / Loop indicator = 1 
			0, 0, // (ushort) number of repetitions (0 = loop forever)
			0, // End of Application Extension block
		};

		private readonly BoardRenderer _Renderer;
		private readonly GifBitmapEncoder _Encoder;

		private readonly string _FilePath;
		private readonly int _Scale;
		private readonly int _DotPitch;
		private readonly int _PixelSize;
		private readonly ushort _GifDelay;

		public GifExporter(string filePath, int scale, int dotPitch, int pixelSize, TimeSpan frameDelay)
		{
			_Renderer = new BoardRenderer();
			_Encoder = new GifBitmapEncoder();
			_FilePath = filePath;
			_Scale = scale;
			_DotPitch = dotPitch;
			_PixelSize = pixelSize;
			_GifDelay = (ushort)(frameDelay.TotalSeconds * 100);
		}

		public void AddFrame(IBoard frame)
		{
			// Render frame
			var bitmap = _Renderer.CreateWriteableBitmap(frame.Width, frame.Height, _DotPitch, _PixelSize);
			_Renderer.RenderBoard(frame, bitmap, _DotPitch, _PixelSize);

			// Resize if necessary
			BitmapSource resized = bitmap;
			if (_Scale > 1) resized = new TransformedBitmap(resized, new ScaleTransform(_Scale, _Scale));

			// Add the GIF frame with specified delay
			var metadata = new BitmapMetadata("gif");
			metadata.SetQuery("/grctlext/Delay", _GifDelay);
			var bmFrame = BitmapFrame.Create(resized, null, metadata, null);
			_Encoder.Frames.Add(bmFrame);
		}

		public void FinalizeImage()
		{
			var gifBytes = new List<byte>();
			using (var memoryStream = new MemoryStream())
			{
				// Encode the GIF and get the raw bytes
				_Encoder.Save(memoryStream);
				var raw = memoryStream.ToArray();

				// Add Header and Logical Screen Descriptor blocks
				gifBytes.AddRange(raw.Take(13));

				// Add Application Extension block
				gifBytes.AddRange(APPEXT);

				// Add GCE and frame data
				gifBytes.AddRange(raw.Skip(13));
			}

			// Write modified buffer to file
			using (var fileStream = new FileStream(_FilePath, FileMode.Create, FileAccess.Write))
			{
				fileStream.Write(gifBytes.ToArray(), 0, gifBytes.Count);
			}
		}
	}
}
