using System.Collections.Generic;

namespace LedBoard.Models.Serialization
{
	public class ProjectModel
	{
		public int Width { get; set; }
		public int Height { get; set; }
		public int FrameDelay { get; set; }
		public ProjectResourceModel[] Resources { get; set; }
		public ProjectStepModel[] Steps { get; set; }
	}

	public class ProjectStepModel
	{
		public ProjectStepModel()
		{
			Configuration = new Dictionary<string, object>();
		}

		public string Type { get; set; }
		public double Duration { get; set; }
		public string ConfigurationType { get; set; }
		public Dictionary<string, object> Configuration { get; set; }
	}

	public class ProjectResourceModel
	{
		public string Uri { get; set; }
		public long FileSize { get; set; }
		public string Signature { get; set; }
	}
}
