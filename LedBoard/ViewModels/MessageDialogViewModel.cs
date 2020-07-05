using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace LedBoard.ViewModels
{
	public class MessageDialogViewModel : DependencyObject
	{
		public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(nameof(Title), typeof(string), typeof(MessageDialogViewModel), new PropertyMetadata(null));

		public string Title
		{
			get => (string)GetValue(TitleProperty);
			set => SetValue(TitleProperty, value);
		}

		public static readonly DependencyProperty MessageProperty = DependencyProperty.Register(nameof(Message), typeof(string), typeof(MessageDialogViewModel), new PropertyMetadata(null));

		public string Message
		{
			get => (string)GetValue(MessageProperty);
			set => SetValue(MessageProperty, value);
		}

		public static readonly DependencyProperty IconTypeProperty = DependencyProperty.Register(nameof(IconType), typeof(MessageDialogIconType), typeof(MessageDialogViewModel), new PropertyMetadata(MessageDialogIconType.None));

		public MessageDialogIconType IconType
		{
			get => (MessageDialogIconType)GetValue(IconTypeProperty);
			set => SetValue(IconTypeProperty, value);
		}

		public static readonly DependencyProperty DetailedMessageProperty = DependencyProperty.Register(nameof(DetailedMessage), typeof(string), typeof(MessageDialogViewModel), new PropertyMetadata(null));

		public string DetailedMessage
		{
			get => (string)GetValue(DetailedMessageProperty);
			set => SetValue(DetailedMessageProperty, value);
		}
	}

	public enum MessageDialogIconType { None, Error, Warning, Info }
}
