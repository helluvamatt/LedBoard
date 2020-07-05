using System.IO;

namespace LedBoard.Services
{
	public interface IResourcesService
	{
		Stream OpenResource(string uri);
		string SaveResource(Stream stream, string filename);
		bool DeleteResource(string uri);

		bool TryGetResourceMeta(string uri, out long filesize, out string signature);
		bool VerifyResource(string uri, long filesize, string signature);

		bool TryGetResourceFileName(string uri, out string filename);
	}
}
