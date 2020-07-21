using LedBoard.Models;
using LedBoard.Services;
using LedBoard.Services.Resources;
using LedBoard.ViewModels;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace LedBoard
{
	/// <summary>
	/// Interaction logic for MasterWindow.xaml
	/// </summary>
	public partial class AppWindow : MetroWindow, IDialogService
	{
		private readonly ProjectResourcesService _ResourcesService;
		private bool _SkipDirty = false;

		public AppWindow()
		{
			_ResourcesService = new ProjectResourcesService(Path.Combine(Path.GetTempPath(), "LedBoard"));
			var vm = new ShellViewModel(this, _ResourcesService);
			vm.SequencePropertyChanged += OnSequencePropertyChanged;
			InitializeComponent();
			DataContext = vm;
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			if (DataContext is ICheckDirty checkDirty && checkDirty.IsDirty && !_SkipDirty)
			{
				e.Cancel = true;
				checkDirty.HandleSessionEnd(OnClose);
			}

			base.OnClosing(e);
		}

		private void OnClose(bool force)
		{
			_SkipDirty = force;
			Close();
		}

		private void OnFrameNavigated(object sender, NavigationEventArgs e)
		{
			// Sync state of Hamburger Menu
			hamburgerMenu.IsPaneOpen = false;
			hamburgerMenu.SelectedItem = hamburgerMenu.Items.OfType<MenuItemViewModel>().FirstOrDefault(item => item.NavigationType == e.Content.GetType());
			hamburgerMenu.SelectedOptionsItem = hamburgerMenu.OptionsItems.OfType<MenuItemViewModel>().FirstOrDefault(item => item.NavigationType == e.Content.GetType());

			// Make sure the child view gets our DataContext
			var frame = (Frame)sender;
			if (frame.Content is FrameworkElement content)
			{
				content.DataContext = DataContext;
			}
		}

		private void OnSequencePropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(Sequence.IsDirty))
			{
				Dispatcher.Invoke(() =>
				{
					var dc = (ShellViewModel)DataContext;
					if (dc.IsDirty)
					{
						Interop.User32.ShutdownBlockReasonCreate(new WindowInteropHelper(this).Handle, "You have unsaved changes to your project.");
					}
					else
					{
						Interop.User32.ShutdownBlockReasonDestroy(new WindowInteropHelper(this).Handle);
					}
				});
			}
		}

		#region IDialogService impl

		private BaseMetroDialog _CurrentDialog;

		public void ShowMessageDialog(string title, string message, MessageDialogIconType icon, string detailedMessage = null)
		{
			Dispatcher.Invoke(async () =>
			{
				var vm = new MessageDialogViewModel
				{
					Title = title,
					Message = message,
					DetailedMessage = detailedMessage,
					IconType = icon,
				};
				_CurrentDialog = (BaseMetroDialog)Resources["MessageDialog"];
				_CurrentDialog.DataContext = vm;
				var settings = new MetroDialogSettings
				{
					AnimateShow = false,
					AnimateHide = false,
				};
				await this.ShowMetroDialogAsync(_CurrentDialog, settings);
			});
		}

		public string OpenFileDialog(string title, string filters, string initialDirectory = null)
		{
			return Dispatcher.Invoke(() =>
			{
				var ofd = new OpenFileDialog
				{
					CheckFileExists = true,
					Filter = filters,
					InitialDirectory = initialDirectory,
					Title = title,
				};
				return (ofd.ShowDialog(this) ?? false) ? ofd.FileName : null;
			});
		}

		public string SaveFileDialog(string title, string filters, string defaultExt = null, bool overwritePrompt = true, string initialDirectory = null)
		{
			return Dispatcher.Invoke(() =>
			{
				var sfd = new SaveFileDialog
				{
					Filter = filters,
					DefaultExt = defaultExt,
					OverwritePrompt = overwritePrompt,
					InitialDirectory = initialDirectory,
					Title = title,
				};
				return (sfd.ShowDialog(this) ?? false) ? sfd.FileName : null;
			});
		}

		public void ConfirmDialog(string title, string message, Action<bool> callback, string affirmativeBtnText = null, string negativeBtnText = null)
		{
			Dispatcher.Invoke(async () =>
			{
				bool result = await ShowConfirmDialog(title, message, affirmativeBtnText, negativeBtnText);
				callback.Invoke(result);
			});
		}

		private async void OnCloseMessageDialog(object sender, RoutedEventArgs e)
		{
			if (_CurrentDialog != null)
			{
				var settings = new MetroDialogSettings
				{
					AnimateShow = false,
					AnimateHide = false,
				};
				await this.HideMetroDialogAsync(_CurrentDialog, settings);
				_CurrentDialog = null;
			}
		}

		private async Task<bool> ShowConfirmDialog(string title, string message, string affirmativeBtnText = null, string negativeBtnText = null)
		{
			var settings = new MetroDialogSettings
			{
				AnimateShow = false,
				AnimateHide = false,
				AffirmativeButtonText = affirmativeBtnText ?? "Yes",
				NegativeButtonText = negativeBtnText ?? "No",
				DefaultButtonFocus = MessageDialogResult.Negative
			};
			return (await this.ShowMessageAsync(title, message, MessageDialogStyle.AffirmativeAndNegative, settings)) == MessageDialogResult.Affirmative;
		}

		public async Task<bool?> ShowConfirmDialogCancelable(string title, string message, string affirmativeBtnText = null, string negativeBtnText = null, string auxBtnText = null)
		{
			var settings = new MetroDialogSettings
			{
				AnimateShow = false,
				AnimateHide = false,
				AffirmativeButtonText = affirmativeBtnText ?? "Yes",
				NegativeButtonText = negativeBtnText ?? "No",
				FirstAuxiliaryButtonText = auxBtnText ?? "Cancel",
				DefaultButtonFocus = MessageDialogResult.FirstAuxiliary,
			};
			var result = await this.ShowMessageAsync(title, message, MessageDialogStyle.AffirmativeAndNegativeAndSingleAuxiliary, settings);
			if (result == MessageDialogResult.FirstAuxiliary) return null;
			return result == MessageDialogResult.Affirmative;
		}

		public async Task<IProgressController> ShowProgressDialogAsync(string title, string message, bool cancelable)
		{
			// Open progress dialog
			var settings = new MetroDialogSettings
			{
				AnimateShow = false,
				AnimateHide = false,
				NegativeButtonText = "Cancel"
			};
			var controller = await this.ShowProgressAsync(title, message, cancelable, settings);
			return new ProgressHandler(controller);
		}

		private class ProgressHandler : IProgressController
		{
			private readonly ProgressDialogController _Controller;

			public ProgressHandler(ProgressDialogController controller)
			{
				_Controller = controller;
			}

			public event EventHandler Canceled
			{
				add => _Controller.Canceled += value;
				remove => _Controller.Canceled -= value;
			}

			public async Task CloseAsync()
			{
				await _Controller.CloseAsync();
			}

			public void Report(double value)
			{
				_Controller.SetProgress(value);
			}

			public void SetIndeterminate()
			{
				_Controller.SetIndeterminate();
			}
		}

		#endregion
	}
}
