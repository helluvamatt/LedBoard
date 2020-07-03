using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Input;

namespace LedBoard.ViewModels.Config
{
	public class TimeSpanAdvancedPropertyViewModel : PropertyViewModel
	{
		private bool _Initializing;

		public TimeSpanAdvancedPropertyViewModel(PropertyInfo property, string label, TimeSpan initialValue, bool showDefault) : base(property, label)
		{
			SetDuration(initialValue);
			ShowDefault = showDefault;
			SetDefaultCommand = new DelegateCommand(() => DefaultRequested?.Invoke(this, EventArgs.Empty));
		}

		public override object Value => new TimeSpan(0, Hours, Minutes, Seconds, Milliseconds);

		public IEnumerable<int> Values12 => Enumerable.Range(0, 13);
		public IEnumerable<int> Values60 => Enumerable.Range(0, 60);

		#region Default value support

		public bool ShowDefault { get; }

		public ICommand SetDefaultCommand { get; }

		public event EventHandler DefaultRequested;

		#endregion

		#region Hours

		public static readonly DependencyProperty HoursProperty = DependencyProperty.Register(nameof(Hours), typeof(int), typeof(TimeSpanAdvancedPropertyViewModel), new PropertyMetadata(0, OnTimeSpanValueChanged));

		public int Hours
		{
			get => (int)GetValue(HoursProperty);
			set => SetValue(HoursProperty, value);
		}

		#endregion

		#region Minutes

		public static readonly DependencyProperty MinutesProperty = DependencyProperty.Register(nameof(Minutes), typeof(int), typeof(TimeSpanAdvancedPropertyViewModel), new PropertyMetadata(0, OnTimeSpanValueChanged));

		public int Minutes
		{
			get => (int)GetValue(MinutesProperty);
			set => SetValue(MinutesProperty, value);
		}

		#endregion

		#region Seconds

		public static readonly DependencyProperty SecondsProperty = DependencyProperty.Register(nameof(Seconds), typeof(int), typeof(TimeSpanAdvancedPropertyViewModel), new PropertyMetadata(0, OnTimeSpanValueChanged));

		public int Seconds
		{
			get => (int)GetValue(SecondsProperty);
			set => SetValue(SecondsProperty, value);
		}

		#endregion

		#region Milliseconds

		public static readonly DependencyProperty MillisecondsProperty = DependencyProperty.Register(nameof(Milliseconds), typeof(int), typeof(TimeSpanAdvancedPropertyViewModel), new PropertyMetadata(0, OnTimeSpanValueChanged));

		public int Milliseconds
		{
			get => (int)GetValue(MillisecondsProperty);
			set => SetValue(MillisecondsProperty, value);
		}

		#endregion

		private static void OnTimeSpanValueChanged(DependencyObject owner, DependencyPropertyChangedEventArgs e)
		{
			var vm = (TimeSpanAdvancedPropertyViewModel)owner;
			if (!vm._Initializing) vm.OnValueChanged();
		}

		public void SetDuration(TimeSpan value)
		{
			_Initializing = true;
			Hours = value.Hours;
			Minutes = value.Minutes;
			Seconds = value.Seconds;
			Milliseconds = value.Milliseconds;
			_Initializing = false;
		}
	}
}
