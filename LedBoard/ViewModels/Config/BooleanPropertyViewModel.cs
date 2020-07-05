using System.Reflection;
using System.Windows;

namespace LedBoard.ViewModels.Config
{
	public class BooleanPropertyViewModel : PropertyViewModel
	{
		public BooleanPropertyViewModel(PropertyInfo property, string label, bool initialValue) : base(property, label)
		{
			Checked = initialValue;
		}

		public override object Value => Checked;

		public static readonly DependencyProperty CheckedProperty = DependencyProperty.Register(nameof(Checked), typeof(bool), typeof(BooleanPropertyViewModel), new PropertyMetadata(false, OnCheckedChanged));

		public bool Checked
		{
			get => (bool)GetValue(CheckedProperty);
			set => SetValue(CheckedProperty, value);
		}

		private static void OnCheckedChanged(DependencyObject owner, DependencyPropertyChangedEventArgs e)
		{
			var vm = (BooleanPropertyViewModel)owner;
			vm.OnValueChanged();
		}
	}
}
