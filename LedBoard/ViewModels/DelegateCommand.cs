using System;
using System.Windows.Input;

namespace LedBoard.ViewModels
{
	public class DelegateCommand<T> : ICommand
	{
		private readonly Predicate<T> _Predicate;
		private readonly Action<T> _Action;

		public DelegateCommand(Action<T> action, Predicate<T> predicate = null)
		{
			_Action = action ?? throw new ArgumentNullException(nameof(action));
			_Predicate = predicate;
		}

		public event EventHandler CanExecuteChanged
		{
			add => CommandManager.RequerySuggested += value;
			remove => CommandManager.RequerySuggested -= value;
		}

		public bool CanExecute(object parameter)
		{
			return _Predicate?.Invoke((T)parameter) ?? true;
		}

		public void Execute(object parameter)
		{
			_Action.Invoke((T)parameter);
		}
	}

	public class DelegateCommand : ICommand
	{
		private readonly Func<bool> _Predicate;
		private readonly Action _Action;

		public DelegateCommand(Action action, Func<bool> predicate = null)
		{
			_Action = action ?? throw new ArgumentNullException(nameof(action));
			_Predicate = predicate;
		}

		public event EventHandler CanExecuteChanged
		{
			add => CommandManager.RequerySuggested += value;
			remove => CommandManager.RequerySuggested -= value;
		}

		public bool CanExecute(object parameter)
		{
			return _Predicate?.Invoke() ?? true;
		}

		public void Execute(object parameter)
		{
			_Action.Invoke();
		}
	}
}
