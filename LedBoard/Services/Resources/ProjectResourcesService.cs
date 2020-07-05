using LedBoard.Models.Serialization;
using System;
using System.IO;
using System.Security.Cryptography;

namespace LedBoard.Services.Resources
{
	public class ProjectResourcesService : IResourcesService
	{
		private const string Scheme = "project";
		private const string Authority = "resources";

		private readonly string _BaseDirectory;

		public ProjectResourcesService(string tempDirectory)
		{
			_BaseDirectory = Path.Combine(tempDirectory, Authority);
			NewProject();
		}

		public void NewProject()
		{
			if (Directory.Exists(_BaseDirectory)) Directory.Delete(_BaseDirectory, true);
			Directory.CreateDirectory(_BaseDirectory);
		}

		public void LoadProject(ProjectModel project, Func<string, Stream> streamCallback)
		{
			NewProject();
			foreach (var resource in project.Resources)
			{
				if (TryParseResourceUri(resource.Uri, out string localPath))
				{
					var stream = streamCallback.Invoke(localPath);
					if (stream != null)
					{
						string filePath = Path.Combine(_BaseDirectory, localPath);
						using (stream)
						{
							// Create resource in temp storage
							Directory.CreateDirectory(Path.GetDirectoryName(filePath));
							using (var targetStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
							{
								stream.CopyTo(targetStream);
							}

							// Verify resource
							using (var resourceStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
							{
								long actualFilesize = resourceStream.Length;
								string actualSignature = ComputeSignature(resourceStream);
								if (actualFilesize != resource.FileSize || actualSignature != resource.Signature)
								{
									throw new InvalidDataException($"Resource \"{resource.Uri}\" failed signature and/or file size verification.");
								}
							}
						}
					}
				}
			}
		}

		public void SaveProject(ProjectModel project, Action<string, Stream> streamCallback)
		{
			foreach (var resource in project.Resources)
			{
				if (TryParseResourceUri(resource.Uri, out string localPath))
				{
					string filePath = Path.Combine(_BaseDirectory, localPath);
					using (var sourceStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
					{
						streamCallback.Invoke(localPath, sourceStream);
					}
				}
			}
		}

		public Stream OpenResource(string uri)
		{
			if (TryParseResourceUri(uri, out string filePath))
			{
				filePath = Path.Combine(_BaseDirectory, filePath);
				if (File.Exists(filePath))
				{
					return new FileStream(filePath, FileMode.Open, FileAccess.Read);
				}
			}
			return null;
		}

		public string SaveResource(Stream stream, string fileName)
		{
			string resourceName = $"{Guid.NewGuid()}/{fileName}";
			var filePath = Path.Combine(_BaseDirectory, resourceName.Replace('/', Path.DirectorySeparatorChar));
			Directory.CreateDirectory(Path.GetDirectoryName(filePath));
			using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
			{
				stream.CopyTo(fileStream);
			}
			return $"{Scheme}://{Authority}/{resourceName}";
		}

		public bool DeleteResource(string uri)
		{
			if (TryParseResourceUri(uri, out string filePath))
			{
				filePath = Path.Combine(_BaseDirectory, filePath);
				if (File.Exists(filePath))
				{
					try
					{
						File.Delete(filePath);
						return true;
					}
					catch (Exception) { }
				}
			}
			return false;
		}

		public bool TryGetResourceMeta(string uri, out long filesize, out string signature)
		{
			Stream stream = OpenResource(uri);
			if (stream != null)
			{
				using (stream)
				{
					filesize = stream.Length;
					signature = ComputeSignature(stream);
					return true;
				}
			}
			filesize = 0;
			signature = null;
			return false;
		}

		public bool VerifyResource(string uri, long filesize, string signature)
		{
			Stream stream = OpenResource(uri);
			if (stream != null)
			{
				using (stream)
				{
					long actualFilesize = stream.Length;
					string actualSignature = ComputeSignature(stream);
					return actualFilesize == filesize && actualSignature == signature;
				}
			}
			return false;
		}

		public bool TryGetResourceFileName(string uri, out string filename)
		{
			if (TryParseResourceUri(uri, out string localPath))
			{
				filename = Path.GetFileName(localPath);
				return true;
			}
			filename = null;
			return false;
		}

		private bool TryParseResourceUri(string uri, out string localPath)
		{
			if (!string.IsNullOrWhiteSpace(uri) && Uri.TryCreate(uri, UriKind.Absolute, out Uri result) && result.Scheme == Scheme && result.Authority == Authority)
			{
				localPath = result.AbsolutePath.Replace('/', Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar);
				return true;
			}
			localPath = null;
			return false;
		}

		private string ComputeSignature(Stream stream)
		{
			SHA512 sha = new SHA512Managed();
			byte[] hash = sha.ComputeHash(stream);
			return Convert.ToBase64String(hash);
		}
	}
}
