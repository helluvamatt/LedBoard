using LedBoard.Models.Text;
using LedBoard.Services;
using System;
using System.ComponentModel;

namespace LedBoard.Models.Steps
{
	public class ClockSequenceStep : SequenceStepBase<ClockConfig>
	{
		private const string Format12Hour = "h:mm:ss tt";
		private const string Format24Hour = "HH:mm:ss";

		public override string DisplayName => "Clock";

		public override TimeSpan DefaultLength => TimeSpan.FromSeconds(5);

		protected override ClockConfig CreateDefaultConfiguration()
		{
			return new ClockConfig
			{
				TimeFormat = TimeFormat.Local12Hour,
				Alignment = Alignment.MiddleCenter,
				Font = FontService.GetDefault(),
				BackgroundColor = 0x222222,
				ForegroundColor = 0xFFFFFF,
			};
		}

		protected override void RenderPreview(IBoard previewBoard)
		{
			var fontRendering = new FontRendering();
			fontRendering.Layout(GetFont(), DateTime.Now.ToShortTimeString());
			fontRendering.RenderText(previewBoard, 1, 4, TypedConfiguration.BackgroundColor, TypedConfiguration.ForegroundColor);
		}

		protected override void OnAnimateFrame(IBoard board)
		{
			string text;

			switch (TypedConfiguration.TimeFormat)
			{
				case TimeFormat.Zulu:
					text = DateTime.UtcNow.ToString(Format24Hour);
					break;
				case TimeFormat.Local24Hour:
					text = DateTime.Now.ToString(Format24Hour);
					break;
				case TimeFormat.Local12Hour:
				default:
					text = DateTime.Now.ToString(Format12Hour);
					break;
			}

			var fontRendering = new FontRendering();
			fontRendering.Layout(GetFont(), text);

			int fontHeight = fontRendering.FontHeight;
			int textWidth = fontRendering.TextWidth;
			int offsetX, offsetY;

			// Compute offsets based on alignment
			if (TypedConfiguration.Alignment.In(Alignment.TopLeft, Alignment.TopCenter, Alignment.TopRight)) offsetY = 1;
			else if (TypedConfiguration.Alignment.In(Alignment.BottomLeft, Alignment.BottomCenter, Alignment.BottomRight)) offsetY = board.Height - fontHeight;
			else offsetY = (board.Height - fontHeight) / 2;

			if (TypedConfiguration.Alignment.In(Alignment.TopLeft, Alignment.MiddleLeft, Alignment.BottomLeft)) offsetX = 1;
			else if (TypedConfiguration.Alignment.In(Alignment.TopRight, Alignment.MiddleRight, Alignment.BottomRight)) offsetX = board.Width - textWidth;
			else offsetX = (board.Width - textWidth) / 2;

			fontRendering.RenderText(board, offsetX, offsetY, TypedConfiguration.BackgroundColor, TypedConfiguration.ForegroundColor);
		}

		private LedFont GetFont() => TypedConfiguration?.Font ?? FontService.GetDefault();
	}

	public class ClockConfig : ICloneable
	{
		[EditorFor("Time Format", Editors.Dropdown)]
		public TimeFormat TimeFormat { get; set; }

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
			return new ClockConfig
			{
				TimeFormat = TimeFormat,
				Font = Font,
				Alignment = Alignment,
				BackgroundColor = BackgroundColor,
				ForegroundColor = ForegroundColor,
			};
		}
	}

	public enum TimeFormat
	{
		[Description("Local Time (12-hour)")]
		Local12Hour,

		[Description("Local Time (24-hour)")]
		Local24Hour,

		[Description("Zulu (UTC) Time")]
		Zulu,
	}
}
