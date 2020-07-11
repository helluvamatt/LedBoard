using Microsoft.Win32;
using System.Collections.Specialized;
using System.Configuration;
using System.Reflection;

namespace LedBoard.Services
{
	internal class RegistrySettingsProvider : SettingsProvider
	{
		public RegistrySettingsProvider()
		{
            ApplicationVendorName = GetType().Assembly.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company;
		}

		public override string ApplicationName
		{
			get => GetType().Assembly.GetName().Name;
			set { } // Do nothing
		}

		public string ApplicationVendorName { get; }

        public override SettingsPropertyValueCollection GetPropertyValues(SettingsContext context, SettingsPropertyCollection props)
        {
            SettingsPropertyValueCollection values = new SettingsPropertyValueCollection();
            using (RegistryKey key = OpenKey(false))
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
            using (RegistryKey key = OpenKey(true))
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

		private RegistryKey OpenKey(bool writable)
		{
            string keyName;
            if (string.IsNullOrWhiteSpace(ApplicationVendorName)) keyName = $"Software\\{ApplicationName}";
            else keyName = $"Software\\{ApplicationVendorName}\\{ApplicationName}";
			return Registry.CurrentUser.CreateSubKey(keyName, writable);
		}
	}
}
