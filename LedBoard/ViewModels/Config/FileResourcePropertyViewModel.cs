using LedBoard.Services;
using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Input;

namespace LedBoard.ViewModels.Config
{
	public class FileResourcePropertyViewModel : PropertyViewModel
	{
		private readonly IDialogService _DialogService;
		private readonly string _FileFilters;

		public FileResourcePropertyViewModel(PropertyInfo property, string label, string initialValue, string fileFilter, IDialogService dialogService) : base(property, label)
		{
			_DialogService = dialogService;
			_FileFilters = fileFilter;
			PickFileCommand = new DelegateCommand(OnPickFile);

			FilePath = initialValue;
		}

		public override object Value => FilePath;

		public ICommand PickFileCommand { get; }

		public static readonly DependencyProperty FilePathProperty = DependencyProperty.Register(nameof(FilePath), typeof(string), typeof(FileResourcePropertyViewModel), new PropertyMetadata(null, OnFilePathChanged));

		public string FilePath
		{
			get => (string)GetValue(FilePathProperty);
			set => SetValue(FilePathProperty, value);
		}

		private static void OnFilePathChanged(DependencyObject owner, DependencyPropertyChangedEventArgs e)
		{
			var vm = (FileResourcePropertyViewModel)owner;
			vm.OnValueChanged();
		}

		private void OnPickFile()
		{
			string result = _DialogService.OpenFileDialog("Choose file...", _FileFilters, FilePath != null ? Path.GetDirectoryName(FilePath) : null);
			if (result != null) FilePath = result;
		}
	}
}
