using System.Reflection;
using System.Windows;

namespace LedBoard.ViewModels.Config
{
	public class TextPropertyViewModel : PropertyViewModel
	{
		public TextPropertyViewModel(PropertyInfo property, string label, string initialValue) : base(property, label)
		{
			Text = initialValue;
		}

		public override object Value => Text;

		public static readonly DependencyProperty TextProperty = DependencyProperty.Register(nameof(Text), typeof(string), typeof(TextPropertyViewModel), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnTextChanged));

		public string Text
		{
			get => (string)GetValue(TextProperty);
			set => SetValue(TextProperty, value);
		}

		private static void OnTextChanged(DependencyObject owner, DependencyPropertyChangedEventArgs e)
		{
			var vm = (TextPropertyViewModel)owner;
			vm.OnValueChanged();
		}
	}
}
