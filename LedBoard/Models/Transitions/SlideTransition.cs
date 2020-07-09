using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LedBoard.Models.Transitions
{
	public class SlideTransition : SequenceTransitionBase<SlideConfig>
	{
		public override string DisplayName => "Slide";

		public override void AnimateFrame(IBoard target, IBoard outgoing, IBoard incoming, TimeSpan frameTime)
		{
			int width = target.Width;
			int height = target.Height;
			double progress = frameTime.TotalMilliseconds / Length.TotalMilliseconds;
			int outgoingX = 0;
			int outgoingY = 0;
			int incomingX = 0;
			int incomingY = 0;
			if (TypedConfiguration.Direction == ImagePanMode.LeftToRight)
			{
				outgoingX = (int)(width * progress);
				incomingX = outgoingX - width;
			}
			else if (TypedConfiguration.Direction == ImagePanMode.TopToBottom)
			{
				outgoingY = (int)(height * progress);
				incomingY = outgoingY - height;
			}
			else if (TypedConfiguration.Direction == ImagePanMode.RightToLeft)
			{
				progress = 1 - progress;
				incomingX = (int)(width * progress);
				outgoingX = incomingX - width;
			}
			else if (TypedConfiguration.Direction == ImagePanMode.BottomToTop)
			{
				progress = 1 - progress;
				incomingY = (int)(height * progress);
				outgoingY = incomingY - height;
			}

			outgoing.CopyTo(target, outgoingX, outgoingY);
			incoming.CopyTo(target, incomingX, incomingY);
		}

		protected override SlideConfig CreateDefaultConfiguration()
		{
			return new SlideConfig
			{
				Direction = ImagePanMode.LeftToRight,
			};
		}
	}

	public class SlideConfig : ICloneable
	{
		[EditorFor("Direction", Editors.Dropdown)]
		public ImagePanMode Direction { get; set; }

		public object Clone()
		{
			return new SlideConfig
			{
				Direction = Direction,
			};
		}
	}
}
