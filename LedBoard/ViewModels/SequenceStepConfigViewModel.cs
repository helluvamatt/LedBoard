using LedBoard.Models;
using LedBoard.Models.Text;
using LedBoard.Services;
using LedBoard.ViewModels.Config;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime;
using System.Windows;

namespace LedBoard.ViewModels
{
	public class SequenceStepConfigViewModel : DependencyObject
	{
		private readonly ISequenceStep _Step;
		private readonly object _CurrentConfiguration;
		private readonly Type _ConfigType;
		private readonly List<PropertyViewModel> _Properties;

		public SequenceStepConfigViewModel(ISequenceStep step, IDialogService dialogService)
		{
			_Step = step ?? throw new ArgumentNullException(nameof(step));
			_CurrentConfiguration = step.CurrentConfiguration;
			_ConfigType = step.ConfigurationType;
			_Properties = new List<PropertyViewModel>();

			if (_CurrentConfiguration != null && _ConfigType != null)
			{
				foreach (PropertyInfo prop in _ConfigType.GetProperties())
				{
					EditorForAttribute editorForAttr = prop.GetCustomAttribute<EditorForAttribute>();
					if (editorForAttr != null)
					{
						PropertyViewModel propVm = null;
						switch (editorForAttr.Editor)
						{
							case Editors.Color:
								propVm = new ColorPropertyViewModel(prop, editorForAttr.Label, (int)prop.GetValue(_CurrentConfiguration));
								break;
							case Editors.Dropdown:
								propVm = new DropdownPropertyViewModel(prop, editorForAttr.Label, prop.GetValue(_CurrentConfiguration));
								break;
							case Editors.Alignment:
								propVm = new AlignmentPropertyViewModel(prop, editorForAttr.Label, (Alignment)prop.GetValue(_CurrentConfiguration));
								break;
							case Editors.LedFont:
								propVm = new LedFontPropertyViewModel(prop, editorForAttr.Label, (LedFont)prop.GetValue(_CurrentConfiguration));
								break;
							case Editors.FileResource:
								propVm = new FileResourcePropertyViewModel(prop, editorForAttr.Label, (string)prop.GetValue(_CurrentConfiguration), editorForAttr.Parameter as string, dialogService);
								break;
							case Editors.TimeSpan:
								propVm = new TimeSpanPropertyViewModel(prop, editorForAttr.Label, (TimeSpan)prop.GetValue(_CurrentConfiguration));
								break;
							case Editors.TimeSpanAdvanced:
								propVm = new TimeSpanAdvancedPropertyViewModel(prop, editorForAttr.Label, (TimeSpan)prop.GetValue(_CurrentConfiguration));
								break;
							case Editors.Text:
							default:
								propVm = new TextPropertyViewModel(prop, editorForAttr.Label, (string)prop.GetValue(_CurrentConfiguration));
								break;
						}
						if (propVm != null)
						{
							propVm.ValueChanged += OnPropertyValueChanged;
							_Properties.Add(propVm);
						}
					}
				}
			}
		}

		public IEnumerable<PropertyViewModel> Properties => _Properties;

		private void OnPropertyValueChanged(object sender, EventArgs e)
		{
			var vm = (PropertyViewModel)sender;
			vm.Property.SetValue(_CurrentConfiguration, vm.Value);
			_Step.Configure(_CurrentConfiguration);
		}
	}

	public abstract class PropertyViewModel : DependencyObject
	{
		protected PropertyViewModel(PropertyInfo property, string label)
		{
			Property = property;
			Label = label;
		}

		public PropertyInfo Property { get; }
		public string Label { get; }

		public abstract object Value { get; }

		#region ValueChanged event

		public event EventHandler ValueChanged;

		protected void OnValueChanged()
		{
			ValueChanged?.Invoke(this, EventArgs.Empty);
		}

		#endregion
	}
}
