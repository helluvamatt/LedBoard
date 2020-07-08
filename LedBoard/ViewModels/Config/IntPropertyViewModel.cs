using System.Reflection;
using System.Windows;

namespace LedBoard.ViewModels.Config
{
	public class IntPropertyViewModel : PropertyViewModel
	{
		public IntPropertyViewModel(PropertyInfo property, string label, int initialValue) : base(property, label)
		{
			IntValue = initialValue;
		}

		public override object Value => IntValue;

		public static readonly DependencyProperty IntValueProperty = DependencyProperty.Register(nameof(IntValue), typeof(int), typeof(IntPropertyViewModel), new PropertyMetadata(0, OnIntValueChanged));

		public int IntValue
		{
			get => (int)GetValue(IntValueProperty);
			set => SetValue(IntValueProperty, value);
		}

		private static void OnIntValueChanged(DependencyObject owner, DependencyPropertyChangedEventArgs e)
		{
			var vm = (IntPropertyViewModel)owner;
			vm.OnValueChanged();
		}
	}
}
