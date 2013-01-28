using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Reflection;
using MediaPod.Interfaces.Models;
using MediaPod.Managers;

namespace MediaPod.Tasks
{
	public class AddToLibrary : BaseTask
	{
		private static int idCounter = 0;

		public AddToLibrary (IFileSystem fileSystem, ITVShow tvShow, TextWriter stdOut=null, TextWriter stdErr=null) : base("AddToLibrary" + idCounter++, stdOut, stdErr)
		{
			_action = () =>
			{
				// Create new tvShow file.
				var newFile = OrganisedLibraryTVShowFile (fileSystem, tvShow);
				LogOutput("Library path {0}", newFile.FullName);
				
				// Check not moving to itself.
				if (tvShow.File.FullName == newFile.FullName)
				{
					LogOutput("Can not move to self.");
					return;
				}
				
				// Create directory if it does not exist.
				if (!newFile.Directory.Exists)
				{
					LogOutput("Creating directory {0}", newFile.Directory.FullName);
					newFile.Directory.Create();
				}
				
				// Copy.
				LogOutput("Copying {0} to {1}", tvShow.File.FullName, newFile.Directory.FullName);
				tvShow.File.CopyTo(newFile.FullName, true);
			};
		}

		private static FileInfoBase OrganisedLibraryTVShowFile(IFileSystem fileSystem, ITVShow tvShow)
		{
			// Initalise empty name.
			var path = "";
			
			// Make clean extension.
			var extension = tvShow.File.Extension.Replace(".", "");
			
			if (tvShow.SeasonNumber.HasValue && !string.IsNullOrEmpty (tvShow.Name))
			{
				// If has SeasonNumber and has EpisodeName.
				path = string.Format ("{0}/Season {1}/{0} - S{1:D2}E{2:D2} - {3}.{4}", tvShow.TVShowName, tvShow.SeasonNumber, tvShow.EpisodeNumber, tvShow.Name, extension);
			}
			else if (tvShow.SeasonNumber.HasValue && string.IsNullOrEmpty (tvShow.Name))
			{
				// If has SeasonNumber and has NO EpisodeName.
				path = string.Format ("{0}/Season {1}/{0} - S{1:D2}E{2:D2}.{3}", tvShow.TVShowName, tvShow.SeasonNumber, tvShow.EpisodeNumber, extension);
			}
			else if (!tvShow.SeasonNumber.HasValue && !string.IsNullOrEmpty (tvShow.Name))
			{
				// If has NO SeasonNumber and has EpisodeName.
				path = string.Format ("{0}/{0} - E{1:D2} - {2}.{3}", tvShow.TVShowName, tvShow.EpisodeNumber, tvShow.Name, extension);
			}
			else if (!tvShow.SeasonNumber.HasValue && string.IsNullOrEmpty (tvShow.Name))
			{
				// If has NO SeasonNumber and has NO EpisodeName.
				path = string.Format ("{0}/{0} - E{1:D2}.{2}", tvShow.TVShowName, tvShow.EpisodeNumber, extension);
			}
			
			// Get full file path and return FileInfo.
			var fullPath = fileSystem.Path.Combine (ResourceManager.TVShowLibrary.RootDirectory.FullName, path);
			return fileSystem.FileInfo.FromFileName (fullPath);
		}
	}
}

