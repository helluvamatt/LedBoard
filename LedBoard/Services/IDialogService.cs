using LedBoard.ViewModels;
using System;
using System.Threading.Tasks;

namespace LedBoard.Services
{
	public interface IDialogService
	{
		void ShowMessageDialog(string title, string message, MessageDialogIconType icon, string detailedMessage = null);
		string OpenFileDialog(string title, string filters, string initialDirectory = null);
		string SaveFileDialog(string title, string filters, string defaultExt = null, bool overwritePrompt = true, string initialDirectory = null);
		void ConfirmDialog(string title, string message, Action<bool> callback, string affirmativeBtnText = null, string negativeBtnText = null);
		Task<IProgressController> ShowProgressDialogAsync(string title, string message, bool cancelable);
		Task<bool?> ShowConfirmDialogCancelable(string title, string message, string affirmativeBtnText = null, string negativeBtnText = null, string auxBtnText = null);
	}

	public interface IProgressController : IProgress<double>
	{
		event EventHandler Canceled;
		void SetIndeterminate();
		Task CloseAsync();
	}
}
