using System;

namespace LedBoard
{
	public interface ICheckDirty
	{
		bool IsDirty { get; }
		void HandleSessionEnd(Action<bool> closeCallback = null);
	}
}
