using LedBoard.Models;
using LedBoard.Services.Rendering;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace LedBoard.Services.Export
{
	public class PngExporter : IExportService
	{
		private readonly BoardRenderer _Renderer;

		private readonly string _FilePath;
		private readonly int _Scale;
		private readonly int _DotPitch;
		private readonly int _PixelSize;

		private int _FrameIndex;

		public PngExporter(string filePath, int scale, int dotPitch, int pixelSize)
		{
			_Renderer = new BoardRenderer();
			_FilePath = filePath;
			_Scale = scale;
			_DotPitch = dotPitch;
			_PixelSize = pixelSize;
			_FrameIndex = 0;
		}

		public void AddFrame(IBoard frame)
		{
			// Render frame
			var bitmap = _Renderer.CreateWriteableBitmap(frame.Width, frame.Height, _DotPitch, _PixelSize);
			_Renderer.RenderBoard(frame, bitmap, _DotPitch, _PixelSize);

			// Resize if necessary
			BitmapSource resized = bitmap;
			if (_Scale > 1) resized = new TransformedBitmap(resized, new ScaleTransform(_Scale, _Scale));

			// Save file
			string filePath = Path.Combine(Path.GetDirectoryName(_FilePath), $"{Path.GetFileNameWithoutExtension(_FilePath)}_{_FrameIndex:000000}{Path.GetExtension(_FilePath)}");
			using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
			{
				var encoder = new PngBitmapEncoder();
				encoder.Frames.Add(BitmapFrame.Create(resized));
				encoder.Save(stream);
			}
			_FrameIndex++;
		}

		public void FinalizeImage() { } // No-op
	}
}
