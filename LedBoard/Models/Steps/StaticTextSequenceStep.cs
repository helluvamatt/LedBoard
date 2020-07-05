using LedBoard.Models.Text;
using LedBoard.Services;
using System;

namespace LedBoard.Models.Steps
{
	public class StaticTextSequenceStep : SequenceStepBase<StaticTextConfig>
	{
		private readonly FontRendering _FontRendering = new FontRendering();

		private int _TextOffsetX;
		private int _TextOffsetY;

		public override string DisplayName => TypedConfiguration != null && !string.IsNullOrWhiteSpace(TypedConfiguration.Text) ? $"Text: {Utils.TrimText(TypedConfiguration.Text, 16)}" : "Text";

		public override TimeSpan DefaultLength => TimeSpan.FromSeconds(5);

		protected override bool OnInit(int width, int height, TimeSpan frameDelay, IResourcesService resourcesService)
		{
			_FontRendering.Layout(TypedConfiguration?.Font ?? FontService.GetDefault(), TypedConfiguration.Text);

			int fontHeight = _FontRendering.FontHeight;
			int textWidth = _FontRendering.TextWidth;

			// Compute offsets based on alignment
			if (TypedConfiguration.Alignment.In(Alignment.TopLeft, Alignment.TopCenter, Alignment.TopRight)) _TextOffsetY = 1;
			else if (TypedConfiguration.Alignment.In(Alignment.BottomLeft, Alignment.BottomCenter, Alignment.BottomRight)) _TextOffsetY = height - fontHeight;
			else _TextOffsetY = (height - fontHeight) / 2;

			if (TypedConfiguration.Alignment.In(Alignment.TopLeft, Alignment.MiddleLeft, Alignment.BottomLeft)) _TextOffsetX = 1;
			else if (TypedConfiguration.Alignment.In(Alignment.TopRight, Alignment.MiddleRight, Alignment.BottomRight)) _TextOffsetX = width - textWidth;
			else _TextOffsetX = (width - textWidth) / 2;

			return true;
		}

		protected override StaticTextConfig CreateDefaultConfiguration()
		{
			return new StaticTextConfig
			{
				Text = string.Empty,
				Alignment = Alignment.MiddleCenter,
				BackgroundColor = 0x222222, // Dark gray
				ForegroundColor = 0xFFFFFF, // White
				Font = FontService.GetDefault(),
			};
		}

		protected override void RenderPreview(IBoard previewBoard)
		{
			_FontRendering.RenderText(previewBoard, 1, 4, TypedConfiguration.BackgroundColor, TypedConfiguration.ForegroundColor);
		}

		public override void AnimateFrame(IBoard board, int step)
		{
			_FontRendering.RenderText(board, _TextOffsetX, _TextOffsetY, TypedConfiguration.BackgroundColor, TypedConfiguration.ForegroundColor);
		}
	}

	public class StaticTextConfig : ICloneable
	{
		[EditorFor("Text", Editors.Text)]
		public string Text { get; set; }

		[EditorFor("Font", Editors.LedFont)]
		public LedFont Font { get; set; }

		[EditorFor("Alignment", Editors.Alignment)]
		public Alignment Alignment { get; set; }

		[EditorFor("Background Color", Editors.Color)]
		public int BackgroundColor { get; set; }

		[EditorFor("Text Color", Editors.Color)]
		public int ForegroundColor { get; set; }

		public object Clone()
		{
			return new StaticTextConfig
			{
				Text = Text,
				Font = Font,
				Alignment = Alignment,
				BackgroundColor = BackgroundColor,
				ForegroundColor = ForegroundColor,
			};
		}
	}
}
