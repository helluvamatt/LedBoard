using System.Reflection;
using System.Windows;
using System.Windows.Input;

namespace LedBoard.ViewModels.Config
{
	public class ColorPropertyViewModel : PropertyViewModel
	{
		public ColorPropertyViewModel(PropertyInfo property, string label, int initialValue) : base(property, label)
		{
			Color = initialValue;
			TogglePopupCommand = new DelegateCommand(() => IsPopupOpen = !IsPopupOpen);
		}

		public override object Value => Color;

		public static readonly DependencyProperty ColorProperty = DependencyProperty.Register(nameof(Color), typeof(int), typeof(ColorPropertyViewModel), new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnColorChanged));

		public int Color
		{
			get => (int)GetValue(ColorProperty);
			set => SetValue(ColorProperty, value);
		}

		private static void OnColorChanged(DependencyObject owner, DependencyPropertyChangedEventArgs e)
		{
			var vm = (ColorPropertyViewModel)owner;
			vm.OnValueChanged();
		}

		public ICommand TogglePopupCommand { get; }

		public static readonly DependencyProperty IsPopupOpenProperty = DependencyProperty.Register(nameof(IsPopupOpen), typeof(bool), typeof(ColorPropertyViewModel), new PropertyMetadata(false));

		public bool IsPopupOpen
		{
			get => (bool)GetValue(IsPopupOpenProperty);
			set => SetValue(IsPopupOpenProperty, value);
		}
	}
}
