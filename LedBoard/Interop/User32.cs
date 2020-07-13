using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace LedBoard.Interop
{
	internal static class User32
	{
		public const int CCHDEVICENAME = 32;

		private const uint MONITORINFOF_PRIMARY = 1;

		[DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		public extern static bool ShutdownBlockReasonCreate([In] IntPtr hWnd, [In] string pwszReason);

		[DllImport("user32.dll", SetLastError = true)]
		public extern static bool ShutdownBlockReasonDestroy([In] IntPtr hWnd);

		[DllImport("user32.dll", SetLastError = true)]
		public static extern bool MoveWindow(IntPtr hWnd, int x, int y, int nWidth, int nHeight, bool bRepaint);

		[DllImport("user32.dll")]
		private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, EnumMonitorsDelegate lpfnEnum, IntPtr dwData);

		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX lpmi);

		public static IEnumerable<MonitorInfo> GetMonitors()
		{
			var monitors = new List<MonitorInfo>();
			int i = 1;
			EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr hMonitor, IntPtr hdcMonitor, ref Rect lprcMonitor, IntPtr dwData) => {
				var mi = new MONITORINFOEX();
				mi.Size = (uint)Marshal.SizeOf(mi);
				bool success = GetMonitorInfo(hMonitor, ref mi);
				if (success) monitors.Add(new MonitorInfo(mi.Monitor, i, (mi.Flags & MONITORINFOF_PRIMARY) == MONITORINFOF_PRIMARY, mi.DeviceName));
				i++;
				return true;
			}, IntPtr.Zero);
			return monitors;
		}
	}

	public class MonitorInfo
	{
		public MonitorInfo(Rect r, int n, bool isPrimary, string deviceName)
		{
			Left = r.left;
			Top = r.top;
			Width = r.right - r.left;
			Height = r.bottom - r.top;
			IsPrimary = isPrimary;
			Number = n;
			DeviceName = deviceName;
		}

		public int Left { get; }
		public int Top { get; }
		public int Width { get; }
		public int Height { get; }

		//public string DisplayName { get; }
		public int Number { get; }
		public bool IsPrimary { get; }
		public string DeviceName { get; }
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
	internal struct MONITORINFOEX
	{
		public uint Size;
		public Rect Monitor;
		public Rect WorkArea;
		public uint Flags;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = User32.CCHDEVICENAME)]
		public string DeviceName;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct Rect
	{
		public int left;
		public int top;
		public int right;
		public int bottom;
	}

	delegate bool EnumMonitorsDelegate(IntPtr hMonitor, IntPtr hdcMonitor, ref Rect lprcMonitor, IntPtr dwData);
}
