using System;

namespace LedBoard.Models.Transitions
{
	public class CrossfadeTransition : SequenceTransitionBase<CrossfadeConfig>
	{
		public override string DisplayName => "Cross-Fade";

		public override void AnimateFrame(IBoard target, IBoard outgoing, IBoard incoming, TimeSpan frameTime)
		{
			double progress = frameTime.TotalMilliseconds / Length.TotalMilliseconds;
			int height = target.Height;
			int width = target.Width;
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					int fromColor = outgoing[x, y];
					int toColor = incoming[x, y];
					byte fromR = (byte)((fromColor >> 16) & 0xFF);
					byte fromG = (byte)((fromColor >> 8) & 0xFF);
					byte fromB = (byte)(fromColor & 0xFF);
					byte toR = (byte)((toColor >> 16) & 0xFF);
					byte toG = (byte)((toColor >> 8) & 0xFF);
					byte toB = (byte)(toColor & 0xFF);
					byte r = (byte)Math.Abs((1 - progress) * fromR + (progress * toR));
					byte g = (byte)Math.Abs((1 - progress) * fromG + (progress * toG));
					byte b = (byte)Math.Abs((1 - progress) * fromB + (progress * toB));
					target[x, y] = r << 16 | g << 8 | b;
				}
			}
		}

		protected override CrossfadeConfig CreateDefaultConfiguration()
		{
			return new CrossfadeConfig();
		}
	}

	public class CrossfadeConfig : ICloneable
	{
		public object Clone()
		{
			return new CrossfadeConfig();
		}
	}
}
