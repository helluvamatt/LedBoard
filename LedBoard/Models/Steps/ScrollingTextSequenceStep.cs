using LedBoard.Services;
using LedBoard.Models.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;

namespace LedBoard.Models.Steps
{
	public class ScrollingTextSequenceStep : SequenceStepBase<ScrollingTextConfig>
	{
		private readonly FontRendering _FontRendering = new FontRendering();

		private int _TextOffsetX;
		private int _TextOffsetY;
		private int _TextWidth;
		private int _StepCount;

		public override string DisplayName => TypedConfiguration != null && !string.IsNullOrWhiteSpace(TypedConfiguration.Text) ? $"Scrolling Text: {Utils.TrimText(TypedConfiguration.Text, 16)}" : "Scrolling Text";

		public override TimeSpan Length => TimeSpan.FromMilliseconds(TypedConfiguration.AnimationSpeed.TotalMilliseconds * _StepCount);

		public override int StepCount => _StepCount;

		public override bool OnInit(int width, int height)
		{
			_FontRendering.Layout(TypedConfiguration?.Font ?? FontService.GetDefault(), TypedConfiguration.Text);
			_TextOffsetY = (height - _FontRendering.FontHeight) / 2;
			_TextOffsetX = width;
			_TextWidth = _FontRendering.TextWidth;
			_StepCount = width + _TextWidth;
			return true;
		}

		protected override ScrollingTextConfig CreateDefaultConfiguration()
		{
			return new ScrollingTextConfig
			{
				Text = string.Empty,
				AnimationSpeed = TimeSpan.FromMilliseconds(100),
				BackgroundColor = 0x222222, // Dark gray
				ForegroundColor = 0xFFFFFF, // White
				Font = FontService.GetDefault(),
			};
		}

		protected override void RenderPreview(IBoard previewBoard)
		{
			_FontRendering.RenderText(previewBoard, 1, 4, TypedConfiguration.BackgroundColor, TypedConfiguration.ForegroundColor);
		}

		public override void AnimateFrame(IBoard board, int step, out TimeSpan afterDelay)
		{
			_FontRendering.RenderText(board, _TextOffsetX - step, _TextOffsetY, TypedConfiguration.BackgroundColor, TypedConfiguration.ForegroundColor);
			afterDelay = TypedConfiguration.AnimationSpeed;
		}
	}

	public class ScrollingTextConfig : ICloneable
	{
		[EditorFor("Text", Editors.Text)]
		public string Text { get; set; }

		[EditorFor("Font", Editors.LedFont)]
		public LedFont Font { get; set; }

		[EditorFor("Frame Duration", Editors.TimeSpan)]
		public TimeSpan AnimationSpeed { get; set; }
		
		[EditorFor("Background Color", Editors.Color)]
		public int BackgroundColor { get; set; }

		[EditorFor("Text Color", Editors.Color)]
		public int ForegroundColor { get; set; }

		public object Clone()
		{
			return new ScrollingTextConfig
			{
				Text = Text,
				Font = Font,
				AnimationSpeed = AnimationSpeed,
				BackgroundColor = BackgroundColor,
				ForegroundColor = ForegroundColor,
			};
		}
	}
}
