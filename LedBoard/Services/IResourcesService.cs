using LedBoard.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;

namespace LedBoard.Services
{
	public interface IResourcesService
	{
		Stream OpenResource(string uri);
		Resource SaveResource(Stream stream, string filename);
		bool DeleteResource(string uri);
		bool CleanupResources(ISet<string> requiredResources);

		bool TryGetResource(string uri, out Resource resource);
		IEnumerable<Resource> Resources { get; }
		event EventHandler<ResourcesChangedEventArgs> ResourceAdded;
		event EventHandler<ResourcesChangedEventArgs> ResourceRemoved;
		event EventHandler ResourcesRefreshed;
	}

	public class ResourcesChangedEventArgs : EventArgs
	{
		public ResourcesChangedEventArgs(Resource subject)
		{
			Subject = subject;
		}

		public Resource Subject { get; }
	}
}
