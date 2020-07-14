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
	}
}
