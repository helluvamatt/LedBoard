using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LedBoard.Models.Transitions
{
	public class WipeTransition : SequenceTransitionBase<WipeConfig>
	{
		public override string DisplayName => "Wipe";

		public override void AnimateFrame(IBoard target, IBoard outgoing, IBoard incoming, TimeSpan frameTime)
		{
			double progress = frameTime.TotalMilliseconds / Length.TotalMilliseconds;
			int width = target.Width;
			int height = target.Height;
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					target[x, y] = GetPixel(outgoing, incoming, x, y, width, height, progress);
				}
			}
		}

		private int GetPixel(IBoard outgoing, IBoard incoming, int x, int y, int w, int h, double progress)
		{
			int threshold;
			if (TypedConfiguration.Direction == ImagePanMode.LeftToRight)
			{
				threshold = (int)(w * progress);
				return x < threshold ? incoming[x, y] : outgoing[x, y];
			}
			else if (TypedConfiguration.Direction == ImagePanMode.TopToBottom)
			{
				threshold = (int)(h * progress);
				return y < threshold ? incoming[x, y] : outgoing[x, y];
			}
			else if (TypedConfiguration.Direction == ImagePanMode.RightToLeft)
			{
				progress = 1 - progress;
				threshold = (int)(w * progress);
				return x > threshold ? incoming[x, y] : outgoing[x, y];
			}
			else if (TypedConfiguration.Direction == ImagePanMode.BottomToTop)
			{
				progress = 1 - progress;
				threshold = (int)(h * progress);
				return y > threshold ? incoming[x, y] : outgoing[x, y];
			}
			return 0;
		}

		protected override WipeConfig CreateDefaultConfiguration()
		{
			return new WipeConfig
			{
				Direction = ImagePanMode.LeftToRight,
			};
		}
	}

	public class WipeConfig : ICloneable
	{
		[EditorFor("Direction", Editors.Dropdown)]
		public ImagePanMode Direction { get; set; }

		public object Clone()
		{
			return new WipeConfig
			{
				Direction = Direction,
			};
		}
	}
}
