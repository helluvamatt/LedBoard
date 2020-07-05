using LedBoard.Models;
using LedBoard.Models.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace LedBoard.Services
{
	internal static class StepService
	{
		private const string Steps_ResourceKey = "steps";

		private static readonly List<StepDescriptor> _StepTypes;

		static StepService()
		{
			_StepTypes = ((ResourceDictionary)Application.Current.Resources[Steps_ResourceKey]).Values.OfType<StepDescriptor>().OrderBy(s => s.SortOrder).ToList();
		}

		public static IEnumerable<StepDescriptor> StepTypes => _StepTypes;

		public static ISequenceStep CreateStep(StepDescriptor descriptor)
		{
			return CreateStep(descriptor.Type);
		}

		public static ISequenceStep CreateStep(ProjectStepModel stepModel)
		{
			var assembly = typeof(ISequenceStep).Assembly;
			var stepType = assembly.GetType(stepModel.Type);
			ISequenceStep step = CreateStep(stepType);
			step.Length = TimeSpan.FromMilliseconds(stepModel.Duration);
			var configType = assembly.GetType(stepModel.ConfigurationType);
			object config = Activator.CreateInstance(configType);
			foreach (var kvp in stepModel.Configuration)
			{
				var prop = configType.GetProperty(kvp.Key);
				prop.SetValue(config, kvp.Value);
			}
			step.Configure(config);
			return step;
		}

		private static ISequenceStep CreateStep(Type stepType)
		{
			return (ISequenceStep)Activator.CreateInstance(stepType);
		}
	}
}
