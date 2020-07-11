using System;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using System.IO.Pipes;

namespace LedBoard.RegistryTool
{
	class Program
	{
		static void Main(string[] args)
		{
			NamedPipeClientStream pipe = null;
			TextWriter writer = Console.Out;
			try
			{
				bool remove = false, showHelp = false;
				string pipeName = null;
				var options = new Mono.Options.OptionSet
				{
					{ "r|remove", "Remove associations", o => remove = o != null },
					{ "p|pipe=", "Named pipe to report output messages", p => pipeName = p },
					{ "h|help", "Show help message", h => showHelp = h != null }
				};
				try
				{
					options.Parse(args);
				}
				catch (Exception ex)
				{
					Console.Error.WriteLine("Failed to parse options:");
					Console.Error.WriteLine(ex.ToString());
				}

				if (showHelp)
				{
					Console.WriteLine("LedBoard.RegistryTool.exe [options]");
					options.WriteOptionDescriptions(Console.Out);
					return;
				}

				if (!string.IsNullOrWhiteSpace(pipeName))
				{
					pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.Out);
					pipe.Connect();
					writer = new StreamWriter(pipe)
					{
						AutoFlush = true,
					};
				}

				string assemblyPath = Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath);
				string resourcesPath = Path.Combine(assemblyPath, "LedBoard.Resources.dll");
				string exePath = Path.Combine(assemblyPath, "LedBoard.exe");

				var registry = Registry.ClassesRoot;
				if (remove)
				{
					registry.DeleteSubKeyTree(".ledproj", false);
					registry.DeleteSubKeyTree("ledboard.project", false);
					InteropShell32.InvalidateIconCache();
					writer.WriteLine("LedBoard project associations removed.");
				}
				else
				{
					using (var key = registry.CreateSubKey("ledboard.project"))
					{
						using (var iconKey = key.CreateSubKey("DefaultIcon"))
						{
							iconKey.SetValue(null, $"{resourcesPath},1", RegistryValueKind.String);
						}
						using (var shellOpenCommandKey = key.CreateSubKey("shell\\open\\command"))
						{
							shellOpenCommandKey.SetValue(null, $"\"{exePath}\" \"%1\"");
						}
						key.SetValue(null, "LedBoard project");
					}
					using (var key = registry.CreateSubKey(".ledproj"))
					{
						key.SetValue(null, "ledboard.project");
					}
					InteropShell32.InvalidateIconCache();
					writer.WriteLine("LedBoard project associations added.");
				}
			}
			catch (Exception ex)
			{
				writer.WriteLine("E:Failed to modify registry:");
				foreach (string line in PrefixLines(ex.ToString(), "E:"))
				{
					writer.WriteLine(line);
				}
				Environment.ExitCode = 1;
			}
			finally
			{
				if (pipe != null)
				{
					pipe.WaitForPipeDrain();
					pipe.Dispose();
				}
			}
		}

		private static IEnumerable<string> PrefixLines(string str, string prefix)
		{
			return str.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(l => $"{prefix}{l}");
		}
	}
}
