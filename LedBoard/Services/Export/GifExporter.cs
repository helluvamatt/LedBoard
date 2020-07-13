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
	/// <summary>
	/// Gif Exporter
	/// </summary>
	/// <remarks>
	/// This classes is based on the current GIF specification: GIF89a
	/// https://www.w3.org/Graphics/GIF/spec-gif89a.txt
	/// </remarks>
	public class GifExporter : IExportService
	{
		private const int GIF_HEADER_SIZE = 6;
		private const int LCT_SIZE = 7;

		private static readonly byte[] GIF_HEADER = new byte[]
		{
			(byte)'G', (byte)'I', (byte)'F', (byte)'8', (byte)'9', (byte)'a', // Header magic: GIF89a
		};

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
		private readonly Stream _Stream;

		private readonly string _FilePath;
		private readonly int _Scale;
		private readonly int _DotPitch;
		private readonly int _PixelSize;
		private readonly byte _MinPixelBrightness;
		private readonly ushort _GifDelay;

		private bool _LsdWritten;
		private bool _AppExtWritten;

		public GifExporter(string filePath, int scale, int dotPitch, int pixelSize, byte minPixelBrightess, TimeSpan frameDelay)
		{
			_Renderer = new BoardRenderer();
			_Stream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
			_FilePath = filePath;
			_Scale = scale;
			_DotPitch = dotPitch;
			_PixelSize = pixelSize;
			_MinPixelBrightness = minPixelBrightess;
			_GifDelay = (ushort)(frameDelay.TotalSeconds * 100);

			// Write GIF header
			_Stream.Write(GIF_HEADER, 0, GIF_HEADER_SIZE);
		}

		public void AddFrame(IBoard frame)
		{
			if (!_LsdWritten)
			{
				ushort pixWidth = (ushort)(_Renderer.GetPixelLength(frame.Width, _DotPitch, _PixelSize) * _Scale);
				ushort pixHeight = (ushort)(_Renderer.GetPixelLength(frame.Height, _DotPitch, _PixelSize) * _Scale);
				WriteLsd(pixWidth, pixHeight);
				_LsdWritten = true;
			}
			if (!_AppExtWritten)
			{
				WriteAppExt();
				_AppExtWritten = true;
			}

			// Render frame
			var bitmap = _Renderer.CreateWriteableBitmap(frame.Width, frame.Height, _DotPitch, _PixelSize);
			_Renderer.RenderBoard(frame, bitmap, _DotPitch, _PixelSize, _MinPixelBrightness);

			// Resize if necessary
			BitmapSource resized = bitmap;
			if (_Scale > 1) resized = new TransformedBitmap(resized, new ScaleTransform(_Scale, _Scale));

			// Write the frame GCE
			WriteGce(_GifDelay);

			// Encode the frame and copy the encoded frame to the stream
			var bmFrame = BitmapFrame.Create(resized);
			var encoder = new GifBitmapEncoder();
			encoder.Frames.Add(bmFrame);
			var gifFrameData = new List<byte>();
			using (var ms = new MemoryStream())
			{
				encoder.Save(ms);
				gifFrameData.AddRange(ms.ToArray().Skip(GIF_HEADER_SIZE + LCT_SIZE));

				// Remove the file closing sentinel (';')
				gifFrameData.RemoveAt(gifFrameData.Count - 1);
			}
			_Stream.Write(gifFrameData.ToArray(), 0, gifFrameData.Count);
		}

		public void FinalizeImage()
		{
			_Stream.WriteByte((byte)';');
			_Stream.Dispose();
		}

		private void WriteLsd(ushort width, ushort height)
		{
			// Logical Screen Width
			_Stream.WriteByte((byte)(width & 0xFF));
			_Stream.WriteByte((byte)((width >> 8) & 0xFF));

			// Logical Screen Height
			_Stream.WriteByte((byte)(height & 0xFF));
			_Stream.WriteByte((byte)((height >> 8) & 0xFF));

			// Packed fields:
			// 1 bit: GCT flag: 0 - No GCT follows
			// 3 bits: Color Resolution: 100 - 3 + 1 bits for original palette
			// 1 bit: Sort Flag: 0 - Not ordered
			// 3 bits: Size of GCT: 110 - 6 -> 2^(6+1) = 128 entries (spec says to set these even if is no GCT specified)
			_Stream.WriteByte(0x70);

			// Background color index (unused - no GCT)
			_Stream.WriteByte(0);

			// Pixel aspect ratio: 0 - No aspect ratio information is given
			_Stream.WriteByte(0);
		}

		private void WriteAppExt()
		{
			_Stream.Write(APPEXT, 0, APPEXT.Length);
		}

		private void WriteGce(ushort frameDelay)
		{
			// Extension Introducer Sentinel ('!')
			_Stream.WriteByte(0x21);
			// GCE label
			_Stream.WriteByte(0xF9);
			// GCE size (4 bytes)
			_Stream.WriteByte(4);
			// Packed fields:
			// 3 bits: Reserved: 0
			// 3 bits: Disposal method: 0 - No action needed
			// 1 bit: User action flag: 0 - No user action needed
			// 1 bit: Transparent color flag: 0 - Transparent index is not given
			_Stream.WriteByte(0);
			// Frame delay (ushort)
			_Stream.WriteByte((byte)(frameDelay & 0xFF));
			_Stream.WriteByte((byte)((frameDelay >> 8) & 0xFF));
			// Transparency index: (unused)
			_Stream.WriteByte(0);
			// End of GCE block
			_Stream.WriteByte(0);
		}
	}
}
