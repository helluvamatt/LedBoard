using LedBoard.Models;
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
			return (ISequenceStep)Activator.CreateInstance(descriptor.Type);
		}
	}
}
