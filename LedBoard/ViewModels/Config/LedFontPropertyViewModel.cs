using LedBoard.Services;
using LedBoard.Models.Text;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;

namespace LedBoard.ViewModels.Config
{
	public class LedFontPropertyViewModel : PropertyViewModel
	{
		public LedFontPropertyViewModel(PropertyInfo property, string label, LedFont initialValue) : base(property, label)
		{
			Font = initialValue;
		}

		public override object Value => Font;

		public IEnumerable<LedFont> Fonts => FontService.GetFonts();

		public static readonly DependencyProperty FontProperty = DependencyProperty.Register(nameof(Font), typeof(LedFont), typeof(LedFontPropertyViewModel), new PropertyMetadata(null, OnFontChanged));

		public LedFont Font
		{
			get => (LedFont)GetValue(FontProperty);
			set => SetValue(FontProperty, value);
		}

		private static void OnFontChanged(DependencyObject owner, DependencyPropertyChangedEventArgs e)
		{
			var vm = (LedFontPropertyViewModel)owner;
			vm.OnValueChanged();
		}
	}
}
