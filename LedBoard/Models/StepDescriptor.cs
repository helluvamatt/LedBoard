using System;
using System.Windows.Media;

namespace LedBoard.Models
{
	public class StepDescriptor
	{
		public string DisplayName { get; set; }
		public int SortOrder { get; set; }
		public Type Type { get; set; }
		public ImageSource Icon { get; set; }
	}
}
