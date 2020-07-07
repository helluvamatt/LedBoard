using System;

namespace LedBoard.Models.Transitions
{
	public class DipToColorTransition : SequenceTransitionBase<DipToColorConfig>
	{
		public override string DisplayName => "Dip to Color";

		public override void AnimateFrame(IBoard target, IBoard outgoing, IBoard incoming, TimeSpan frameTime)
		{
			double progress = frameTime.TotalMilliseconds / Length.TotalMilliseconds;
			double curve = Math.Abs(progress - 0.5) * 2;
			byte dipR = (byte)((TypedConfiguration.Color >> 16) & 0xFF);
			byte dipG = (byte)((TypedConfiguration.Color >> 8) & 0xFF);
			byte dipB = (byte)(TypedConfiguration.Color & 0xFF);
			IBoard source = progress < 0.5 ? outgoing : incoming;
			int height = target.Height;
			int width = target.Width;
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					int c = source[x, y];
					byte r = (byte)((c >> 16) & 0xFF);
					byte g = (byte)((c >> 8) & 0xFF);
					byte b = (byte)(c & 0xFF);
					r = (byte)Math.Abs((curve * r) + ((1 - curve) * dipR));
					g = (byte)Math.Abs((curve * g) + ((1 - curve) * dipG));
					b = (byte)Math.Abs((curve * b) + ((1 - curve) * dipB));
					target[x, y] = r << 16 | g << 8 | b;
				}
			}
		}

		protected override DipToColorConfig CreateDefaultConfiguration()
		{
			return new DipToColorConfig
			{
				Color = 0, // Black
			};
		}
	}

	public class DipToColorConfig : ICloneable
	{
		[EditorFor("Color", Editors.Color)]
		public int Color { get; set; }

		public object Clone()
		{
			return new DipToColorConfig
			{
				Color = Color,
			};
		}
	}
}
