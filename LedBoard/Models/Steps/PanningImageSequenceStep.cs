using LedBoard.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace LedBoard.Models.Steps
{
	public class PanningImageSequenceStep : SequenceStepBase<PanningImageConfig>
	{
		private IBoard _ViewportImage;
		private int _AvailableSteps;

		protected override bool SupportsAnimation => true;

		public override string DisplayName => "Panning Image";
		public override TimeSpan DefaultLength => TimeSpan.FromSeconds(5);
		public override IEnumerable<string> Resources => new string[] { TypedConfiguration.Image };

		protected override bool OnInit(int width, int height, IResourcesService resourcesService)
		{
			var imageStream = resourcesService.OpenResource(TypedConfiguration.Image);
			if (imageStream != null)
			{
				using (imageStream)
				{
					using (var bitmap = new Bitmap(imageStream))
					{
						// Compute real width and height
						float ratio;
						int w = width, h = height;
						if (TypedConfiguration.PanMode == ImagePanMode.BottomToTop || TypedConfiguration.PanMode == ImagePanMode.TopToBottom)
						{
							// Width is same, change height
							ratio = (float)w / bitmap.Width;
							h = (int)(bitmap.Height * ratio);
							_AvailableSteps = Math.Abs(h - height);
						}
						else
						{
							// Height is same, change width
							ratio = (float)h / bitmap.Height;
							w = (int)(bitmap.Width * ratio);
							_AvailableSteps = Math.Abs(w - width);
						}

						// Generate and resize the image
						_ViewportImage = new MemoryBoard(w, h);
						using (var resized = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format24bppRgb))
						{
							using (var g = Graphics.FromImage(resized))
							{
								g.Clear(Color.Black);
								switch (TypedConfiguration.ResizeQuality)
								{
									case ImageResizeQuality.NearestNeighbor:
										g.InterpolationMode = InterpolationMode.NearestNeighbor;
										break;
									case ImageResizeQuality.Bilinear:
										g.InterpolationMode = InterpolationMode.Bilinear;
										break;
									case ImageResizeQuality.HighQualityBilinear:
										g.InterpolationMode = InterpolationMode.HighQualityBilinear;
										break;
									case ImageResizeQuality.Bicubic:
										g.InterpolationMode = InterpolationMode.Bicubic;
										break;
									case ImageResizeQuality.HighQualityBicubic:
										g.InterpolationMode = InterpolationMode.HighQualityBicubic;
										break;
									default:
										g.InterpolationMode = InterpolationMode.Default;
										break;
								}
								Rectangle destRect = new Rectangle(0, 0, w, h);
								Rectangle srcRect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
								g.DrawImage(bitmap, destRect, srcRect, GraphicsUnit.Pixel);
							}

							Color pixel;
							for (int y = 0; y < h; y++)
							{
								for (int x = 0; x < w; x++)
								{
									pixel = resized.GetPixel(x, y);
									_ViewportImage[x, y] = (pixel.R << 16) | (pixel.G << 8) | pixel.B;
								}
							}
						}
					}
				}
			}
			return true;
		}

		protected override void OnAnimateFrame(IBoard board, TimeSpan frameTime, TimeSpan transitionExtra)
		{
			if (_ViewportImage != null)
			{
				board.SetAll(TypedConfiguration.BackgroundColor);
				int step = ComputeStep(_AvailableSteps, frameTime, transitionExtra);
				if (step > _AvailableSteps) step = _AvailableSteps;
				int srcX = 0, srcY = 0, srcW = _ViewportImage.Width, srcH = _ViewportImage.Height;
				int dstX = 0, dstY = 0, dstW = board.Width, dstH = board.Height;
				switch (TypedConfiguration.PanMode)
				{
					case ImagePanMode.TopToBottom:
						if (srcH > dstH) srcY += step;
						else dstY += step;
						break;
					case ImagePanMode.BottomToTop:
						if (srcH > dstH) srcY = srcH - dstH - step;
						else dstY = dstH - srcH - step;
						break;
					case ImagePanMode.LeftToRight:
						if (srcW > dstW) srcX += step;
						else dstX += step;
						break;
					case ImagePanMode.RightToLeft:
						if (srcW > dstW) srcX = srcW - dstW - step;
						else dstX = dstW - srcW - step;
						break;
				}
				_ViewportImage.CopySubset(board, srcX, srcY, srcW, srcH, dstX, dstY);
			}
		}

		protected override PanningImageConfig CreateDefaultConfiguration()
		{
			return new PanningImageConfig
			{
				PanMode = ImagePanMode.TopToBottom,
				ResizeQuality = ImageResizeQuality.HighQualityBicubic,
				BackgroundColor = 0, // Black
			};
		}

		protected override void RenderPreview(IBoard previewBoard)
		{
			if (_ViewportImage != null)
			{
				_ViewportImage.CopySubset(previewBoard, 0, 0, Math.Min(_ViewportImage.Width, previewBoard.Width), Math.Min(_ViewportImage.Height, previewBoard.Height), 0, 0);
			}
		}
	}

	public class PanningImageConfig : ICloneable
	{
		[EditorFor("Image", Editors.FileResource, Parameter = "Image files|*.png;*.jpg;*.jpeg;*.gif;*.bmp|PNG images|*.png|JPEG images|*.jpg;*.jpeg|GIF images|*.gif|BMP images|*.bmp")]
		public string Image { get; set; }

		[EditorFor("Pan Direction", Editors.Dropdown)]
		public ImagePanMode PanMode { get; set; }

		[EditorFor("Resize Quality", Editors.Dropdown)]
		public ImageResizeQuality ResizeQuality { get; set; }

		[EditorFor("Background Color", Editors.Color)]
		public int BackgroundColor { get; set; }

		public object Clone()
		{
			return new PanningImageConfig
			{
				Image = Image,
				PanMode = PanMode,
				ResizeQuality = ResizeQuality,
				BackgroundColor = BackgroundColor,
			};
		}
	}
}
