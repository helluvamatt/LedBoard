using LedBoard.Models;
using LedBoard.Models.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace LedBoard.Services
{
	internal static class StepService
	{
		private const string Steps_ResourceKey = "steps";
		private const string Transitions_ResourceKey = "transitions";

		private static readonly List<StepDescriptor> _StepTypes;
		private static readonly List<TransitionDescriptor> _TransitionTypes;

		static StepService()
		{
			_StepTypes = ((ResourceDictionary)Application.Current.Resources[Steps_ResourceKey]).Values.OfType<StepDescriptor>().OrderBy(s => s.SortOrder).ToList();
			_TransitionTypes = ((ResourceDictionary)Application.Current.Resources[Transitions_ResourceKey]).Values.OfType<TransitionDescriptor>().OrderBy(t => t.SortOrder).ToList();
		}

		public static IEnumerable<StepDescriptor> StepTypes => _StepTypes;
		public static IEnumerable<TransitionDescriptor> TransitionTypes => _TransitionTypes;

		public static ISequenceStep CreateStep(StepDescriptor descriptor)
		{
			return CreateItem<ISequenceStep>(descriptor.Type);
		}

		public static ISequenceStep CreateStep(ProjectStepModel stepModel)
		{
			var assembly = typeof(ISequenceStep).Assembly;
			var stepType = assembly.GetType(stepModel.Type);
			ISequenceStep step = CreateItem<ISequenceStep>(stepType);
			step.Length = TimeSpan.FromMilliseconds(stepModel.Duration);
			var configType = assembly.GetType(stepModel.ConfigurationType);
			object config = Activator.CreateInstance(configType);
			foreach (var kvp in stepModel.Configuration)
			{
				var prop = configType.GetProperty(kvp.Key);
				if (prop != null) prop.SetValue(config, kvp.Value);
			}
			step.Configure(config);
			return step;
		}

		public static ISequenceTransition CreateTransition(TransitionDescriptor descriptor)
		{
			return CreateItem<ISequenceTransition>(descriptor.Type);
		}

		public static ISequenceTransition CreateTransition(ProjectTransitionModel transitionModel)
		{
			var assembly = typeof(ISequenceTransition).Assembly;
			var transitionType = assembly.GetType(transitionModel.Type);
			ISequenceTransition transition = CreateItem<ISequenceTransition>(transitionType);
			transition.Length = TimeSpan.FromMilliseconds(transitionModel.Duration);
			var configType = assembly.GetType(transitionModel.ConfigurationType);
			object config = Activator.CreateInstance(configType);
			foreach (var kvp in transitionModel.Configuration)
			{
				var prop = configType.GetProperty(kvp.Key);
				if (prop != null) prop.SetValue(config, kvp.Value);
			}
			transition.Configure(config);
			return transition;
		}

		private static T CreateItem<T>(Type type) where T : ISequenceItem
		{
			return (T)Activator.CreateInstance(type);
		}

		public static ImageSource GetIconForTransition(ISequenceTransition transition)
		{
			if (transition == null) return null;
			return _TransitionTypes.FirstOrDefault(td => td.Type == transition.GetType())?.Icon;
		}
	}
}
