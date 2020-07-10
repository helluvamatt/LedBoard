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
			IncreaseCommand = new DelegateCommand<int>(OnIncrease, _ => TsValue < TimeSpan.FromHours(12));
			DecreaseCommand = new DelegateCommand<int>(OnDecrease, _ => TsValue > TimeSpan.Zero);
		}

		public override object Value => TsValue;

		public IEnumerable<int> Values12 => Enumerable.Range(0, 13);
		public IEnumerable<int> Values60 => Enumerable.Range(0, 60);

		public ICommand IncreaseCommand { get; }
		public ICommand DecreaseCommand { get; }

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

		public static readonly DependencyProperty MillisecondsProperty = DependencyProperty.Register(nameof(Milliseconds), typeof(int), typeof(TimeSpanAdvancedPropertyViewModel), new PropertyMetadata(0, OnTimeSpanValueChanged), ValidateMilliseconds);

		public int Milliseconds
		{
			get => (int)GetValue(MillisecondsProperty);
			set => SetValue(MillisecondsProperty, value);
		}

		private static bool ValidateMilliseconds(object value)
		{
			return value is int val && val >= 0 && val < 1000;
		}

		#endregion

		private static void OnTimeSpanValueChanged(DependencyObject owner, DependencyPropertyChangedEventArgs e)
		{
			var vm = (TimeSpanAdvancedPropertyViewModel)owner;
			if (!vm._Initializing) vm.OnValueChanged();
		}

		public void SetDuration(TimeSpan value, bool fireUpdate = false)
		{
			_Initializing = true;
			Hours = value.Hours;
			Minutes = value.Minutes;
			Seconds = value.Seconds;
			Milliseconds = value.Milliseconds;
			_Initializing = false;
			CommandManager.InvalidateRequerySuggested();
			if (fireUpdate) OnValueChanged();
		}

		private void OnIncrease(int repeatCount)
		{
			TimeSpan delta;
			if (repeatCount > 1000) delta = TimeSpan.FromSeconds(1);
			else if (repeatCount > 100) delta = TimeSpan.FromMilliseconds(100);
			else if (repeatCount > 10) delta = TimeSpan.FromMilliseconds(10);
			else delta = TimeSpan.FromMilliseconds(1);

			var ts = TsValue + delta;
			if (ts <= TimeSpan.FromHours(12)) SetDuration(ts, true);
		}

		private void OnDecrease(int repeatCount)
		{
			TimeSpan delta;
			if (repeatCount > 1000) delta = TimeSpan.FromSeconds(1);
			else if (repeatCount > 100) delta = TimeSpan.FromMilliseconds(100);
			else if (repeatCount > 10) delta = TimeSpan.FromMilliseconds(10);
			else delta = TimeSpan.FromMilliseconds(1);

			var ts = TsValue - delta;
			if (ts >= TimeSpan.Zero) SetDuration(ts, true);
		}

		private TimeSpan TsValue => new TimeSpan(0, Hours, Minutes, Seconds, Milliseconds);
	}
}
