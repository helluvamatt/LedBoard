using System;

namespace LedBoard.Models
{
	public class Resource
	{
		public Resource(string name, Uri uri)
		{
			Name = name;
			Uri = uri;
		}

		public string Name { get; }
		public Uri Uri { get; }
	}
}
