using LedBoard.Screensaver.Properties;
using LedBoard.Services;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Forms;

namespace LedBoard.Screensaver
{
	public partial class ConfigForm : Form, INotifyPropertyChanged
	{
		private readonly string _AppPath;

		private string _ImagePath = null;

		public ConfigForm()
		{
			InitializeComponent();
			if (!string.IsNullOrWhiteSpace(Settings.Default.ImagePath)) ImagePath = Settings.Default.ImagePath;

			// Read the registry to find where LedBoard.exe is located
			using (var key = RegistrySettingsProvider.OpenAppSettingsKey(false))
			{
				_AppPath = key.GetValue(RegistrySettingsProvider.AppPathValueName) as string;
				if (string.IsNullOrWhiteSpace(_AppPath)) btnLaunchApp.Enabled = false;
			}
		}

		public string ImagePath
		{
			get => _ImagePath ?? Resources.DefaultBrowseStr;
			set
			{
				if (_ImagePath != value)
				{
					_ImagePath = value;
					OnPropertyChanged(nameof(ImagePath));
				}
			}
		}

		#region Event handlers

		private void OnOkClick(object sender, EventArgs e)
		{
			// Save settings
			Settings.Default.ImagePath = _ImagePath;
			Settings.Default.Save();

			DialogResult = DialogResult.OK;
			Close();
		}

		private void OnCancelClick(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}

		private void OnBrowseClick(object sender, EventArgs e)
		{
			if (openFileDialog.ShowDialog(this) == DialogResult.OK)
			{
				ImagePath = openFileDialog.FileName;
			}
		}

		private void OnOpenAppClick(object sender, EventArgs e)
		{
			if (!string.IsNullOrWhiteSpace(_AppPath))
			{
				Process.Start(_AppPath);
			}
		}

		#endregion

		#region INotifyPropertyChanged impl

		public event PropertyChangedEventHandler PropertyChanged;

		private void OnPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		#endregion
	}
}
