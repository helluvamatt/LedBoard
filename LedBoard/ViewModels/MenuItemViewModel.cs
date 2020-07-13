using MahApps.Metro.Controls;
using System;
using System.Windows;

namespace LedBoard.ViewModels
{
	public class MenuItemViewModel : HamburgerMenuIconItem
	{
		public static readonly DependencyProperty NavigationTypeProperty = DependencyProperty.Register(nameof(NavigationType), typeof(Type), typeof(MenuItemViewModel), new PropertyMetadata(null));

		public Type NavigationType
		{
			get => (Type)GetValue(NavigationTypeProperty);
			set => SetValue(NavigationTypeProperty, value);
		}

		public static readonly DependencyProperty NavigationTargetProperty = DependencyProperty.Register(nameof(NavigationTarget), typeof(Uri), typeof(MenuItemViewModel), new PropertyMetadata(null));

		public Uri NavigationTarget
		{
			get => (Uri)GetValue(NavigationTargetProperty);
			set => SetValue(NavigationTargetProperty, value);
		}

		//public static readonly DependencyProperty IconProperty = DependencyProperty.Register(nameof(Icon), typeof(object), typeof(MenuItemViewModel), new PropertyMetadata(null));

		//public object Icon
		//{
		//	get => (object)GetValue(IconProperty);
		//	set => SetValue(IconProperty, value);
		//}

		//public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(nameof(Label), typeof(string), typeof(MenuItemViewModel), new PropertyMetadata(null));

		//public string Label
		//{
		//	get => (string)GetValue(LabelProperty);
		//	set => SetValue(LabelProperty, value);
		//}
	}
}
