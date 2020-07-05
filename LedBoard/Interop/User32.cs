using System;
using System.Runtime.InteropServices;

namespace LedBoard.Interop
{
	internal static class User32
	{
		[DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		public extern static bool ShutdownBlockReasonCreate([In] IntPtr hWnd, [In] string pwszReason);

		[DllImport("user32.dll", SetLastError = true)]
		public extern static bool ShutdownBlockReasonDestroy([In] IntPtr hWnd);
	}
}
