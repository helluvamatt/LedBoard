using LedBoard.Services;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace LedBoard.ViewModels.Config
{
	public class FileResourcePropertyViewModel : PropertyViewModel
	{
		private readonly IDialogService _DialogService;
		private readonly IResourcesService _ResourcesService;
		private readonly string _FileFilters;

		private string _Uri;

		public FileResourcePropertyViewModel(PropertyInfo property, string label, string initialValue, string fileFilter, IDialogService dialogService, IResourcesService resourcesService) : base(property, label)
		{
			_DialogService = dialogService;
			_ResourcesService = resourcesService;
			_FileFilters = fileFilter;
			PickFileCommand = new DelegateCommand(OnPickFile);
			_Uri = initialValue;
			if (_ResourcesService.TryGetResourceFileName(initialValue, out string filename))
			{
				FilePath = filename;
			}
		}

		public override object Value => _Uri;

		public ICommand PickFileCommand { get; }

		public static readonly DependencyProperty FilePathProperty = DependencyProperty.Register(nameof(FilePath), typeof(string), typeof(FileResourcePropertyViewModel), new PropertyMetadata(null));

		public string FilePath
		{
			get => (string)GetValue(FilePathProperty);
			set => SetValue(FilePathProperty, value);
		}

		private void OnPickFile()
		{
			string result = _DialogService.OpenFileDialog("Choose file...", _FileFilters, FilePath != null ? Path.GetDirectoryName(FilePath) : null);
			if (result != null)
			{
				Task.Run(() =>
				{
					try
					{
						if (_Uri != null)
						{
							_ResourcesService.DeleteResource(_Uri);
							_Uri = null;
						}

						string filename;
						using (var stream = new FileStream(result, FileMode.Open, FileAccess.Read))
						{
							filename = Path.GetFileName(result);
							_Uri = _ResourcesService.SaveResource(stream, filename);
						}
						Dispatcher.Invoke(() =>
						{
							FilePath = filename;
							OnValueChanged();
						});
					}
					catch (Exception ex)
					{
						_DialogService.ShowMessageDialog("Error", $"Failed to save resource: {ex.Message}", MessageDialogIconType.Error, ex.ToString());
					}
				});
				
			}
		}
	}
}
