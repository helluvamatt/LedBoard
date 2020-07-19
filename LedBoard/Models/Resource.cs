using System;

namespace LedBoard.Models
{
	public class Resource
	{
		public Resource(Uri uri, string path, string name, long fileSize, string signature)
		{
			Uri = uri ?? throw new ArgumentNullException(nameof(uri));
			Path = path ?? throw new ArgumentNullException(nameof(path));
			Name = name ?? throw new ArgumentNullException(nameof(name));
			Filesize = fileSize;
			Signature = signature ?? throw new ArgumentNullException(nameof(signature));
		}

		public Uri Uri { get; }
		public string Path { get; }
		public string Name { get; }
		public long Filesize { get; }
		public string Signature { get; }

		public override string ToString() => Name;
		public override bool Equals(object obj) => obj is Resource other && other.Uri == Uri;
		public override int GetHashCode() => Uri.GetHashCode();

	}
}
