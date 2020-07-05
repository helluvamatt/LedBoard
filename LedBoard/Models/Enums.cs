using System.ComponentModel;

namespace LedBoard.Models
{
	#region Images

	public enum ImageResizeMode
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

	public enum ImageResizeQuality
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

	public enum ImagePanMode
	{
		[Description("Top to Bottom")]
		TopToBottom,
		[Description("Bottom to Top")]
		BottomToTop,
		[Description("Left to Right")]
		LeftToRight,
		[Description("Right to Left")]
		RightToLeft,
	}

	#endregion

	#region Text

	public enum Alignment
	{
		[Description("Top Left")]
		TopLeft,
		[Description("Top Center")]
		TopCenter,
		[Description("Top Right")]
		TopRight,
		[Description("Middle Left")]
		MiddleLeft,
		[Description("Middle Center")]
		MiddleCenter,
		[Description("Middle Right")]
		MiddleRight,
		[Description("Bottom Left")]
		BottomLeft,
		[Description("Bottom Center")]
		BottomCenter,
		[Description("Bottom Right")]
		BottomRight
	}

	#endregion
}
