using LedBoard.Models;
using System.Reflection;
using System.Windows;

namespace LedBoard.ViewModels.Config
{
	public class AlignmentPropertyViewModel : PropertyViewModel
	{
		public AlignmentPropertyViewModel(PropertyInfo property, string label, Alignment initialValue) : base(property, label)
		{
			Alignment = initialValue;
		}

		public override object Value => Alignment;

		public static readonly DependencyProperty AlignmentProperty = DependencyProperty.Register(nameof(Alignment), typeof(Alignment), typeof(AlignmentPropertyViewModel), new PropertyMetadata(Alignment.MiddleCenter, OnAlignmentPropertyChanged));

		public Alignment Alignment
		{
			get => (Alignment)GetValue(AlignmentProperty);
			set => SetValue(AlignmentProperty, value);
		}

		private static void OnAlignmentPropertyChanged(DependencyObject owner, DependencyPropertyChangedEventArgs e)
		{
			var vm = (AlignmentPropertyViewModel)owner;
			vm.OnValueChanged();
		}
	}
}
