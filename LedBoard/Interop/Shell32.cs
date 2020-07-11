using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace LedBoard.Interop
{
	public static class Shell32
	{
		public static IEnumerable<string> GetMRUFiles(string filespec)
		{
			var recentFiles = new List<string>();
			var basePath = Environment.GetFolderPath(Environment.SpecialFolder.Recent);
			var baseDir = new DirectoryInfo(basePath);
			var files = baseDir.GetFiles($"{filespec}.lnk").OrderByDescending(fi => fi.LastWriteTimeUtc);
			if (!files.Any()) return recentFiles;
			var shellType = Type.GetTypeFromProgID("Wscript.Shell");
			dynamic script = Activator.CreateInstance(shellType);
			foreach (var file in files)
			{
				dynamic sc = script.CreateShortcut(file.FullName);
				recentFiles.Add(sc.TargetPath);
				Marshal.FinalReleaseComObject(sc);
			}
			Marshal.FinalReleaseComObject(script);
			return recentFiles;
		}

		public static void AddToRecentlyUsedDocs(string path)
		{
			SHAddToRecentDocs(ShellAddToRecentDocsFlags.Path, path);
		}

		private enum ShellAddToRecentDocsFlags
		{
			Pidl = 0x001,
			Path = 0x002,
		}

		[DllImport("shell32.dll", CharSet = CharSet.Ansi)]
		private static extern void SHAddToRecentDocs(ShellAddToRecentDocsFlags flag, string path);
	}
}
