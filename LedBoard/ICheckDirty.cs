namespace LedBoard
{
	public interface ICheckDirty
	{
		bool IsDirty { get; }
		void HandleSessionEnd();
	}
}
