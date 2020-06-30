using System;

namespace LedBoard.Services
{
	public interface IDialogService
	{
		void ShowMessageDialog(string title, string message);
		string OpenFileDialog(string title, string filters, string initialDirectory = null);
		void ConfirmDialog(string title, string message, Action<bool> callback, string affirmativeBtnText = null, string negativeBtnText = null);
	}
}
