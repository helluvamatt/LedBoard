using MahApps.Metro.Controls;
using MahApps.Metro.IconPacks;
using System.Windows;

namespace LedBoard.Controls
{
	internal static class ControlHelpers
	{
		public static readonly DependencyProperty IconProperty = DependencyProperty.RegisterAttached("Icon", typeof(PackIconFontAwesomeKind), typeof(ControlHelpers), new PropertyMetadata(PackIconFontAwesomeKind.None));

		[AttachedPropertyBrowsableForType(typeof(UIElement))]
		public static PackIconFontAwesomeKind GetIcon(UIElement obj)
		{
			return (PackIconFontAwesomeKind)obj.GetValue(IconProperty);
		}

		public static void SetIcon(UIElement obj, PackIconFontAwesomeKind value)
		{
			obj.SetValue(IconProperty, value);
		}
	}
}
