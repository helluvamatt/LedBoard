using LedBoard.Models.Serialization;
using LedBoard.Services.Resources;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Windows.Markup;

namespace LedBoard.Services
{
	internal class ProjectService
	{
		private readonly ProjectResourcesService _ResourcesService;

		public ProjectService(ProjectResourcesService resourcesService)
		{
			_ResourcesService = resourcesService;
		}

		public ProjectModel LoadProject(string path)
		{
			using (var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read))
			{
				using (var archive = new ZipArchive(fileStream, ZipArchiveMode.Read))
				{
					var projectEntry = archive.GetEntry("project.xml");
					if (projectEntry == null) throw new InvalidDataException("Project file is missing project.xml.");
					ProjectModel model;
					using (var pomXmlStream = projectEntry.Open())
					{
						model = DeserializeProject(pomXmlStream);
					}
					_ResourcesService.LoadProject(model, zipPath =>
					{
						zipPath = zipPath.Replace(Path.DirectorySeparatorChar, '/');
						var entry = archive.GetEntry($"resources/{zipPath}");
						if (entry != null) return entry.Open();
						return null;
					});
					return model;
				}
			}
		}

		public IEnumerable<string> SaveProject(ProjectModel project, IDictionary<string, IEnumerable<string>> requiredResources, string path)
		{
			using (var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write))
			{
				using (var archive = new ZipArchive(fileStream, ZipArchiveMode.Create))
				{
					var errors = _ResourcesService.SaveProject(project, requiredResources, (localPath, src) =>
					{
						localPath = localPath.Replace(Path.DirectorySeparatorChar, '/');
						var resourceEntry = archive.CreateEntry($"resources/{localPath}");
						using (var zipStream = resourceEntry.Open())
						{
							src.CopyTo(zipStream);
						}
					});

					if (!errors.Any())
					{
						var projectEntry = archive.CreateEntry("project.xml");
						using (var pomXmlStream = projectEntry.Open())
						{
							SerializeProject(project, pomXmlStream);
						}
					}

					return errors;
				}
			}
		}

		private void SerializeProject(ProjectModel project, Stream stream)
		{
			using (var writer = new StreamWriter(stream, Encoding.UTF8))
			{
				XamlWriter.Save(project, writer);
			}
		}

		private ProjectModel DeserializeProject(Stream stream)
		{
			return (ProjectModel)XamlReader.Load(stream);
		}
	}
}
