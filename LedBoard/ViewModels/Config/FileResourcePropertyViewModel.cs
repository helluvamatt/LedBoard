using LedBoard.Models;
using LedBoard.Services;
using System;
using System.Collections.ObjectModel;
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

		public FileResourcePropertyViewModel(PropertyInfo property, string label, string initialValue, string fileFilter, IDialogService dialogService, IResourcesService resourcesService) : base(property, label)
		{
			_DialogService = dialogService;
			_ResourcesService = resourcesService;
			_FileFilters = fileFilter;
			PickFileCommand = new DelegateCommand(OnPickFile);
			CurrentResources = new ObservableCollection<Resource>();
			PopulateResources();
			if (_ResourcesService.TryGetResource(initialValue, out Resource resource))
			{
				Resource = resource;
			}
		}

		public override object Value => Resource.Uri.AbsoluteUri;

		public ICommand PickFileCommand { get; }

		public ObservableCollection<Resource> CurrentResources { get; }

		public static readonly DependencyProperty ResourceProperty = DependencyProperty.Register(nameof(Resource), typeof(Resource), typeof(FileResourcePropertyViewModel), new PropertyMetadata(null, OnResourceChanged));

		public Resource Resource
		{
			get => (Resource)GetValue(ResourceProperty);
			set => SetValue(ResourceProperty, value);
		}

		private static void OnResourceChanged(DependencyObject owner, DependencyPropertyChangedEventArgs e)
		{
			var vm = (FileResourcePropertyViewModel)owner;
			vm.OnValueChanged();
		}

		private void OnPickFile()
		{
			string result = _DialogService.OpenFileDialog("Choose file...", _FileFilters, null);
			if (result != null)
			{
				Task.Run(() =>
				{
					try
					{
						using (var stream = new FileStream(result, FileMode.Open, FileAccess.Read))
						{
							Resource newResource = _ResourcesService.SaveResource(stream, Path.GetFileName(result));
							if (newResource != null)
							{
								Dispatcher.Invoke(() =>
								{
									PopulateResources();
									Resource = newResource;
								});
							}
						}
					}
					catch (Exception ex)
					{
						_DialogService.ShowMessageDialog("Error", $"Failed to save resource: {ex.Message}", MessageDialogIconType.Error, ex.ToString());
					}
				});
			}
		}

		private void PopulateResources()
		{
			CurrentResources.Clear();
			foreach (var resource in _ResourcesService.Resources)
			{
				CurrentResources.Add(resource);
			}
		}
	}
}
