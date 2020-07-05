using LedBoard.ViewModels;
using System;

namespace LedBoard.Services
{
	public interface IDialogService
	{
		void ShowMessageDialog(string title, string message, MessageDialogIconType icon, string detailedMessage = null);
		string OpenFileDialog(string title, string filters, string initialDirectory = null);
		string SaveFileDialog(string title, string filters, string initialDirectory = null);
		void ConfirmDialog(string title, string message, Action<bool> callback, string affirmativeBtnText = null, string negativeBtnText = null);
	}
}
