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
			Configuration = new ProjectConfigurationMap();
		}

		public string Type { get; set; }
		public double Duration { get; set; }
		public string ConfigurationType { get; set; }
		public ProjectConfigurationMap Configuration { get; set; }
		public ProjectTransitionModel Transition { get; set; }
	}

	public class ProjectResourceModel
	{
		public string Uri { get; set; }
		public long FileSize { get; set; }
		public string Signature { get; set; }
	}

	public class ProjectTransitionModel
	{
		public ProjectTransitionModel()
		{
			Configuration = new ProjectConfigurationMap();
		}

		public string Type { get; set; }
		public double Duration { get; set; }
		public string ConfigurationType { get; set; }
		public ProjectConfigurationMap Configuration { get; set; }
	}

	public class ProjectConfigurationMap : Dictionary<string, object>
	{
		public ProjectConfigurationMap() : base() { }

		public ProjectConfigurationMap(IDictionary<string, object> values) : base(values) { }
	}
}
