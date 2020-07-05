using LedBoard.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace LedBoard.Models.Steps
{
	public class StaticImageSequenceStep : SequenceStepBase<StaticImageConfig>
	{
		public override string DisplayName => "Static Image";
		public override TimeSpan DefaultLength => TimeSpan.FromSeconds(5);
		public override IEnumerable<string> Resources => new string[] { TypedConfiguration.Image };

		private IBoard _StaticImage;

		protected override bool OnInit(int width, int height, TimeSpan frameDelay, IResourcesService resourcesService)
		{
			_StaticImage = new MemoryBoard(width, height);
			var imageStream = resourcesService.OpenResource(TypedConfiguration.Image);
			if (imageStream != null)
			{
				using (imageStream)
				{
					using (var bitmap = new Bitmap(imageStream))
					{
						using (var resized = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format24bppRgb))
						{
							using (var g = Graphics.FromImage(resized))
							{
								g.Clear(Color.Black);
								switch (TypedConfiguration.ResizeQuality)
								{
									case StaticImageResizeQuality.NearestNeighbor:
										g.InterpolationMode = InterpolationMode.NearestNeighbor;
										break;
									case StaticImageResizeQuality.Bilinear:
										g.InterpolationMode = InterpolationMode.Bilinear;
										break;
									case StaticImageResizeQuality.HighQualityBilinear:
										g.InterpolationMode = InterpolationMode.HighQualityBilinear;
										break;
									case StaticImageResizeQuality.Bicubic:
										g.InterpolationMode = InterpolationMode.Bicubic;
										break;
									case StaticImageResizeQuality.HighQualityBicubic:
										g.InterpolationMode = InterpolationMode.HighQualityBicubic;
										break;
									default:
										g.InterpolationMode = InterpolationMode.Default;
										break;
								}
								Rectangle destRect;
								Rectangle srcRect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
								float ratio;
								int destX, destY, destW, destH;
								switch (TypedConfiguration.ResizeMode)
								{
									case StaticImageResizeMode.Stretch:
										destRect = new Rectangle(0, 0, width, height);
										break;
									case StaticImageResizeMode.Crop:
										destRect = new Rectangle(0, 0, width, height);
										srcRect = destRect;
										break;
									case StaticImageResizeMode.Center:
										ratio = Math.Min((float)width / srcRect.Width, (float)height / srcRect.Height);
										destW = (int)(srcRect.Width * ratio);
										destH = (int)(srcRect.Height * ratio);
										destX = (width - destW) / 2;
										destY = (height - destH) / 2;
										destRect = new Rectangle(destX, destY, destW, destH);
										break;
									case StaticImageResizeMode.Fit:
									default:
										ratio = Math.Max((float)width / srcRect.Width, (float)height / srcRect.Height);
										destW = (int)(srcRect.Width * ratio);
										destH = (int)(srcRect.Height * ratio);
										destX = (width - destW) / 2;
										destY = (height - destH) / 2;
										destRect = new Rectangle(destX, destY, destW, destH);
										break;
								}
								g.DrawImage(bitmap, destRect, srcRect, GraphicsUnit.Pixel);
							}

							Color pixel;
							for (int y = 0; y < height; y++)
							{
								for (int x = 0; x < width; x++)
								{
									pixel = resized.GetPixel(x, y);
									_StaticImage[x, y] = (pixel.R << 16) | (pixel.G << 8) | pixel.B;
								}
							}
						}
					}
				}
			}
			return true;
		}

		public override void AnimateFrame(IBoard board, int step)
		{
			if (_StaticImage != null) board.CopyFrom(_StaticImage);
		}

		protected override StaticImageConfig CreateDefaultConfiguration()
		{
			return new StaticImageConfig()
			{
				ResizeMode = StaticImageResizeMode.Fit,
				ResizeQuality = StaticImageResizeQuality.HighQualityBicubic,
			};
		}

		protected override void RenderPreview(IBoard previewBoard)
		{
			if (_StaticImage != null)
			{
				_StaticImage.CopySubset(previewBoard, 0, 0, Math.Min(_StaticImage.Width, previewBoard.Width), Math.Min(_StaticImage.Height, previewBoard.Height), 0, 0);
			}
		}
	}

	public class StaticImageConfig : ICloneable
	{
		[EditorFor("Image", Editors.FileResource, Parameter = "Image files|*.png;*.jpg;*.jpeg;*.gif;*.bmp|PNG images|*.png|JPEG images|*.jpg;*.jpeg|GIF images|*.gif|BMP images|*.bmp")]
		public string Image { get; set; }

		[EditorFor("Resize Mode", Editors.Dropdown)]
		public StaticImageResizeMode ResizeMode { get; set; }

		[EditorFor("Resize Quality", Editors.Dropdown)]
		public StaticImageResizeQuality ResizeQuality { get; set; }

		public object Clone()
		{
			return new StaticImageConfig
			{
				Image = Image,
				ResizeMode = ResizeMode,
				ResizeQuality = ResizeQuality,
			};
		}
	}

	public enum StaticImageResizeMode
	{
		[Description("Crop")]
		Crop,
		[Description("Resize and Center (Preserves Aspect Ratio)")]
		Center,
		[Description("Resize and Fit (Preserves Aspect Ratio)")]
		Fit,
		[Description("Stretch (May Distort Image)")]
		Stretch,
	}

	public enum StaticImageResizeQuality
	{
		[Description("Nearest Neighbor (Fastest, Lowest Quality)")]
		NearestNeighbor,
		[Description("Bilinear (Fast, Acceptable Quality)")]
		Bilinear,
		[Description("High-Quality Bilinear (Average Speed, Decent Quality)")]
		HighQualityBilinear,
		[Description("Bicubic (Slow, Better Quality)")]
		Bicubic,
		[Description("High-Quality Bicubic (Slowest, Best Quality)")]
		HighQualityBicubic,
	}
}
