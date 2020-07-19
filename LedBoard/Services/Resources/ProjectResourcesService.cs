using LedBoard.Models;
using LedBoard.Models.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace LedBoard.Services.Resources
{
	public class ProjectResourcesService : IResourcesService
	{
		private const string Scheme = "project";
		private const string Authority = "resources";

		private readonly string _BaseDirectory;
		private readonly Dictionary<string, ProjectResource> _Resources;

		public ProjectResourcesService(string tempDirectory)
		{
			_BaseDirectory = Path.Combine(tempDirectory, Authority);
			_Resources = new Dictionary<string, ProjectResource>();
			NewProject(false);
		}

		#region NewProject

		public void NewProject()
		{
			NewProject(true);
		}

		private void NewProject(bool fireEvents)
		{
			if (Directory.Exists(_BaseDirectory)) Directory.Delete(_BaseDirectory, true);
			Directory.CreateDirectory(_BaseDirectory);
			_Resources.Clear();
			if (fireEvents) ResourcesRefreshed?.Invoke(this, EventArgs.Empty);
		}

		#endregion

		#region LoadProject and SaveProject

		public void LoadProject(ProjectModel project, Func<string, Stream> streamCallback)
		{
			NewProject(false);
			foreach (var resource in project.Resources)
			{
				if (TryParseResourceUri(resource.Uri, out Uri uri, out string localPath))
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

						// Track resource
						string name = Path.GetFileName(localPath);
						var localResource = new ProjectResource(uri, filePath, name, resource.FileSize, resource.Signature, localPath);
						_Resources.Add(uri.AbsoluteUri, localResource);
					}
					else
					{
						throw new InvalidDataException($"Resource \"{resource.Uri}\" is missing from project.");
					}
				}
				else
				{
					throw new InvalidDataException($"Resource URI \"{resource.Uri}\" is invalid.");
				}
			}

			ResourcesRefreshed?.Invoke(this, EventArgs.Empty);
		}

		public IEnumerable<string> SaveProject(ProjectModel project, IDictionary<string, IEnumerable<string>> requiredResources, Action<string, Stream> streamCallback)
		{
			// Verify all required resources are present
			var errorList = new List<string>();
			foreach (var kvp in requiredResources)
			{
				if (!_Resources.ContainsKey(kvp.Key))
				{
					errorList.Add($"{string.Join(", ", kvp.Value)}: {kvp.Key}");
				}
			}
			if (errorList.Any()) return errorList;

			// Build resource models
			var resourceList = new List<ProjectResourceModel>();
			foreach (var resource in _Resources.Values)
			{
				resourceList.Add(new ProjectResourceModel
				{
					Uri = resource.Uri.AbsoluteUri,
					FileSize = resource.Filesize,
					Signature = resource.Signature,
				});
					
				using (var sourceStream = new FileStream(resource.Path, FileMode.Open, FileAccess.Read))
				{
					streamCallback.Invoke(resource.LocalPath, sourceStream);
				}
			}

			// Store resource references in project
			project.Resources = resourceList.ToArray();

			return Enumerable.Empty<string>();
		}

		#endregion

		#region IResourceService impl

		public IEnumerable<Resource> Resources => _Resources.Values;

		public event EventHandler<ResourcesChangedEventArgs> ResourceAdded;
		public event EventHandler<ResourcesChangedEventArgs> ResourceRemoved;
		public event EventHandler ResourcesRefreshed;

		public Stream OpenResource(string uri)
		{
			if (uri != null && _Resources.TryGetValue(uri, out ProjectResource resource))
			{
				if (File.Exists(resource.Path))
				{
					return new FileStream(resource.Path, FileMode.Open, FileAccess.Read);
				}
			}
			return null;
		}

		public Resource SaveResource(Stream stream, string fileName)
		{
			string resourceName = $"{Guid.NewGuid()}/{fileName}";
			if (Uri.TryCreate($"{Scheme}://{Authority}/{resourceName}", UriKind.Absolute, out Uri uri))
			{
				string localPath = resourceName.Replace('/', Path.DirectorySeparatorChar);
				string filePath = Path.Combine(_BaseDirectory, localPath);
				Directory.CreateDirectory(Path.GetDirectoryName(filePath));
				using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite))
				{
					// Extract resource file to temp storage
					stream.CopyTo(fileStream);

					// Read meta
					fileStream.Position = 0;
					if (ReadMeta(fileStream, out long filesize, out string signature))
					{
						// Track resource
						var localResource = new ProjectResource(uri, filePath, fileName, filesize, signature, localPath);
						_Resources.Add(uri.AbsoluteUri, localResource);
						ResourceAdded?.Invoke(this, new ResourcesChangedEventArgs(localResource));

						// Return local resource instance
						return localResource;
					}
				}
			}

			return null;
		}

		public bool DeleteResource(string uri)
		{
			if (uri != null && _Resources.TryGetValue(uri, out ProjectResource resource))
			{
				try
				{
					if (File.Exists(resource.Path))
					{
						File.Delete(resource.Path);
						_Resources.Remove(uri);
						ResourceRemoved?.Invoke(this, new ResourcesChangedEventArgs(resource));
						return true;
					}
				}
				catch (Exception) { }
			}
			return false;
		}

		public bool CleanupResources(ISet<string> requiredResources)
		{
			var keysToRemove = _Resources.Keys.Except(requiredResources);
			foreach (var uri in keysToRemove)
			{
				if (!DeleteResource(uri)) return false;
			}
			return keysToRemove.Any();
		}

		public bool VerifyResource(string uri, long filesize, string signature)
		{
			Stream stream = OpenResource(uri);
			return ReadMeta(stream, out long actualFilesize, out string actualSignature) && actualFilesize == filesize && actualSignature == signature;
		}

		public bool TryGetResource(string uri, out Resource resource)
		{
			if (uri != null && _Resources.TryGetValue(uri, out ProjectResource projResource))
			{
				resource = projResource;
				return true;
			}
			resource = null;
			return false;
		}

		#endregion

		private bool TryParseResourceUri(string uri, out Uri uriObj, out string localPath)
		{
			if (!string.IsNullOrWhiteSpace(uri) && Uri.TryCreate(uri, UriKind.Absolute, out uriObj) && uriObj.Scheme == Scheme && uriObj.Authority == Authority)
			{
				localPath = uriObj.AbsolutePath.Replace('/', Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar);
				return true;
			}
			localPath = null;
			uriObj = null;
			return false;
		}

		private bool ReadMeta(Stream stream, out long filesize, out string signature)
		{
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

		private string ComputeSignature(Stream stream)
		{
			SHA512 sha = new SHA512Managed();
			byte[] hash = sha.ComputeHash(stream);
			return Convert.ToBase64String(hash);
		}

		#region Resource class

		private class ProjectResource : Resource
		{
			public ProjectResource(Uri uri, string fullPath, string name, long fileSize, string signature, string localPath) : base(uri, fullPath, name, fileSize, signature)
			{
				LocalPath = localPath ?? throw new ArgumentNullException(localPath);
			}

			public string LocalPath { get; }
		}

		#endregion
	}
}
