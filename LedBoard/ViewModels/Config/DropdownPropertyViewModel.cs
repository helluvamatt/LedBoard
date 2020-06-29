using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;

namespace LedBoard.ViewModels.Config
{
	public class DropdownPropertyViewModel : PropertyViewModel
	{
		private readonly List<EnumItem> _Options;

		public DropdownPropertyViewModel(PropertyInfo property, string label, object initialValue) : base(property, label)
		{
			SelectedItem = initialValue;

			_Options = new List<EnumItem>();
			if (property.PropertyType.IsEnum)
			{
				_Options.AddRange(Enum.GetValues(property.PropertyType).Cast<object>().Select(v => CreateItem(property.PropertyType, v)));
			}
		}

		public override object Value => SelectedItem;

		public IEnumerable<EnumItem> Options => _Options;

		public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register(nameof(SelectedItem), typeof(object), typeof(DropdownPropertyViewModel), new PropertyMetadata(null, OnSelectedItemChanged));

		public object SelectedItem
		{
			get => GetValue(SelectedItemProperty);
			set => SetValue(SelectedItemProperty, value);
		}

		private static void OnSelectedItemChanged(DependencyObject owner, DependencyPropertyChangedEventArgs e)
		{
			var vm = (DropdownPropertyViewModel)owner;
			vm.OnValueChanged();
		}

		private EnumItem CreateItem(Type enumType, object value)
		{
			string description = value.ToString();
			var descriptionAttr = enumType.GetField(description).GetCustomAttribute<DescriptionAttribute>();
			if (descriptionAttr != null) description = descriptionAttr.Description;
			return new EnumItem(value, description);
		}
	}

	public class EnumItem
	{
		public EnumItem(object value, string description)
		{
			Value = value;
			Description = description;
		}

		public object Value { get; }
		public string Description { get; }
	}
}
