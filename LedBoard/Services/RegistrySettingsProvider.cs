using Microsoft.Win32;
using System.Collections.Specialized;
using System.Configuration;
using System.Reflection;

namespace LedBoard.Services
{
	internal class RegistrySettingsProvider : SettingsProvider
	{
		#region Statics

		public const string AppPathValueName = "AppPath";
		public const string AppName = "LedBoard";

		private readonly static string _ExeName;
		private readonly static string _AppKeyName;
		private readonly static string _ExeKeyName;

		static RegistrySettingsProvider()
		{
			var assembly = typeof(RegistrySettingsProvider).Assembly;
			string vendorName = assembly.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company;
			_ExeName = assembly.GetName().Name;
			if (string.IsNullOrWhiteSpace(vendorName)) _AppKeyName = $"Software\\{AppName}";
			else _AppKeyName = $"Software\\{vendorName}\\{AppName}";
			_ExeKeyName = $"{_AppKeyName}\\{_ExeName}";
		}

		public static RegistryKey OpenAppSettingsKey(bool writable)
		{
			return Registry.CurrentUser.CreateSubKey(_AppKeyName, writable);
		}

		#endregion

		public override string ApplicationName
		{
			get => _ExeName;
			set { } // Do nothing
		}

        public override SettingsPropertyValueCollection GetPropertyValues(SettingsContext context, SettingsPropertyCollection props)
        {
            SettingsPropertyValueCollection values = new SettingsPropertyValueCollection();
            using (RegistryKey key = OpenSettingsKey(false))
            {
                foreach (SettingsProperty setting in props)
                {
					values.Add(new SettingsPropertyValue(setting)
					{
						IsDirty = false,
						SerializedValue = key.GetValue(setting.Name)
					});
                }
            }
            return values;
        }

        public override void SetPropertyValues(SettingsContext context, SettingsPropertyValueCollection properties)
        {
            using (RegistryKey key = OpenSettingsKey(true))
            {
                foreach (SettingsPropertyValue value in properties)
                {
                    key.SetValue(value.Name, value.SerializedValue ?? "", RegistryValueKind.String);
                }
            }
        }

		public override void Initialize(string name, NameValueCollection config)
		{
			base.Initialize(GetType().Name, config);
		}

		private RegistryKey OpenSettingsKey(bool writable)
		{
			return Registry.CurrentUser.CreateSubKey(_ExeKeyName, writable);
		}
	}
}
