using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;

namespace LedBoard.ViewModels.Config
{
	public class TimeSpanPropertyViewModel : PropertyViewModel
	{
		public TimeSpanPropertyViewModel(PropertyInfo property, string label, TimeSpan initialValue) : base(property, label)
		{
			TimeSpanValue = initialValue;
			Options = new List<TimeSpanDescriptor>
			{
				new TimeSpanDescriptor(TimeSpan.FromMilliseconds(10), "Very Fast"),
				new TimeSpanDescriptor(TimeSpan.FromMilliseconds(50), "Fast"),
				new TimeSpanDescriptor(TimeSpan.FromMilliseconds(100), "Normal"),
				new TimeSpanDescriptor(TimeSpan.FromMilliseconds(500), "Slow"),
				new TimeSpanDescriptor(TimeSpan.FromMilliseconds(1000), "Very Slow"),
			};
		}

		public override object Value => TimeSpanValue;

		public IEnumerable<TimeSpanDescriptor> Options { get; }

		public static readonly DependencyProperty TimeSpanValueProperty = DependencyProperty.Register(nameof(TimeSpanValue), typeof(TimeSpan), typeof(TimeSpanPropertyViewModel), new FrameworkPropertyMetadata(TimeSpan.Zero, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnTimeSpanValueChanged));

		public TimeSpan TimeSpanValue
		{
			get => (TimeSpan)GetValue(TimeSpanValueProperty);
			set => SetValue(TimeSpanValueProperty, value);
		}

		private static void OnTimeSpanValueChanged(DependencyObject owner, DependencyPropertyChangedEventArgs e)
		{
			var vm = (TimeSpanPropertyViewModel)owner;
			vm.OnValueChanged();
		}
	}

	public class TimeSpanDescriptor
	{
		public TimeSpanDescriptor(TimeSpan value, string description)
		{
			Value = value;
			Description = description;
		}

		public TimeSpan Value { get; }
		public string Description { get; }
	}
}
