using System;
using System.Collections.Generic;

namespace LedBoard.Models.Transitions
{
	public class DissolveTransition : SequenceTransitionBase<DissolveConfig>
	{
		public override string DisplayName => "Dissolve";

		private readonly List<bool[]> _Maps = new List<bool[]>();

		protected override bool OnInit(int width, int height)
		{
			_Maps.Clear();
			var random = new Random(TypedConfiguration.RandomSeed);
			int stepCount = (int)(Length.TotalMilliseconds / _FrameDelay.TotalMilliseconds);
			int totalPixels = width * height;
			int pixelsPerStep = totalPixels / stepCount;
			bool[] prevLayer = new bool[totalPixels];
			bool[] layer;
			int px, cur;
			for (int i = 0; i < stepCount; i++)
			{
				layer = new bool[totalPixels];
				Array.Copy(prevLayer, layer, totalPixels);
				px = 0;
				while (px < pixelsPerStep)
				{
					cur = random.Next(totalPixels);
					if (!layer[cur])
					{
						layer[cur] = true;
						px++;
					}
				}
				_Maps.Add(layer);
				prevLayer = layer;
			}
			return true;
		}

		public override void AnimateFrame(IBoard target, IBoard outgoing, IBoard incoming, TimeSpan frameTime)
		{
			double progress = frameTime.TotalMilliseconds / Length.TotalMilliseconds;
			int step = (int)(progress * _Maps.Count);
			if (step < 0) step = 0;
			if (step > _Maps.Count - 1) step = _Maps.Count - 1;
			int width = target.Width;
			int height = target.Height;
			target.CopyFrom(outgoing);
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					if (_Maps[step][y * width + x]) target[x, y] = incoming[x, y];
				}
			}
		}

		protected override DissolveConfig CreateDefaultConfiguration()
		{
			return new DissolveConfig
			{
				RandomSeed = 0
			};
		}
	}

	public class DissolveConfig : ICloneable
	{
		[EditorFor("Random Seed", Editors.Integer)]
		public int RandomSeed { get; set; }

		public object Clone()
		{
			return new DissolveConfig
			{
				RandomSeed = RandomSeed,
			};
		}
	}
}
