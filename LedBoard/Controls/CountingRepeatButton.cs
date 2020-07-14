using System.Windows;
using System.Windows.Controls.Primitives;

namespace LedBoard.Controls
{
	public class CountingRepeatButton : RepeatButton
	{
		private int _RepeatCount;

		public CountingRepeatButton() : base()
		{
			CommandParameter = 0;
		}

		protected override void OnClick()
		{
			base.OnClick();
			CommandParameter = ++_RepeatCount;
		}

		protected override void OnIsPressedChanged(DependencyPropertyChangedEventArgs e)
		{
			CommandParameter = _RepeatCount = 0;
			base.OnIsPressedChanged(e);
		}
	}
}
