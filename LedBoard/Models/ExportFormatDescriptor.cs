namespace LedBoard.Models
{
	public class ExportFormatDescriptor
	{
		public ExportFormat Value { get; set; }
		public string Text { get; set; }
		public string DefaultExt { get; set; }
		public string Filters { get; set; }
	}

	public enum ExportFormat { GIF, PNGSeries, APNG }
}
