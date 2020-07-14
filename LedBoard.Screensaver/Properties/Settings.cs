using System.Configuration;
using LedBoard.Services;

namespace LedBoard.Screensaver.Properties
{
    [SettingsProvider(typeof(RegistrySettingsProvider))]
    internal sealed partial class Settings { }
}
