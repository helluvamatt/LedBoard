using System;
using System.Runtime.InteropServices;

namespace LedBoard.RegistryTool
{
	internal static class InteropShell32
	{
		private const uint SHCNE_ASSOCCHANGED = 0x08000000;
		private const ushort SHCNF_IDLIST = 0x0000;

		public static void InvalidateIconCache()
		{
			SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);
		}

		[DllImport("Shell32")]
		private static extern void SHChangeNotify(uint wEventId, ushort flags, IntPtr dwItem1, IntPtr dwItem2);
	}
}
