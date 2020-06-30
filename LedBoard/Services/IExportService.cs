using LedBoard.Models;

namespace LedBoard.Services
{
	public interface IExportService
	{
		void AddFrame(IBoard frame);
		void FinalizeImage();
	}
}
