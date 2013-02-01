using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Reflection;
using MediaPod.Interfaces.Models;
using MediaPod.Managers;

namespace MediaPod.Tasks
{
	public class RemuxEncodeMetadataAndAddToLibrary : BaseTask
	{
		private static int idCounter = 0;

		public RemuxEncodeMetadataAndAddToLibrary (IFileSystem fileSystem, ITVShow tvShow, bool deleteOriginal, TextWriter stdOut=null, TextWriter stdErr=null) : base("RemuxEncodeMetadataAndAddToLibrary" + idCounter++, stdOut, stdErr)
		{
			_action = () =>
			{
				// Create and set working area.
				var workingArea = fileSystem.DirectoryInfo.FromDirectoryName (fileSystem.Path.Combine (fileSystem.Path.Combine (fileSystem.Path.GetTempPath (), Assembly.GetExecutingAssembly().GetName().Name), this.GetType().Name));
				if (!workingArea.Exists)
				{
					LogOutput("Creating working area at {0}.", workingArea.FullName);
					workingArea.Create();
				}

				// Make working area file.
				var name = tvShow.File.Name.Replace(tvShow.File.Extension, "") + ".m4v";
				var workingAreaFile = fileSystem.FileInfo.FromFileName(fileSystem.Path.Combine(workingArea.FullName, name));

				// Make working area tvShow.
				var workingAreaTVShow = new WorkingTVShow(tvShow, workingAreaFile);

				// Remux.
				var remuxTask = new Remux(fileSystem, tvShow.File, workingAreaFile, StdOut, StdErr);
				LogOutput("Starting remux sub-task {0}.", remuxTask.Id);
				remuxTask.StdOut = StdOut;
				remuxTask.StdErr = StdErr;
				remuxTask.Invoke();

				// Encode.
				var encodeTask = new EncodeMetadata(fileSystem, workingAreaTVShow, StdOut, StdErr);
				LogOutput("Starting encoding sub-task {0}.", encodeTask.Id);
				encodeTask.StdOut = StdOut;
				encodeTask.StdErr = StdErr;
				encodeTask.Invoke();

				// Add to library.
				var addToLibrary = new AddToLibrary(fileSystem, workingAreaTVShow, StdOut, StdErr);
				LogOutput("Starting add to library sub-task {0}.", addToLibrary.Id);
				addToLibrary.StdOut = StdOut;
				addToLibrary.StdErr = StdErr;
				addToLibrary.Invoke();

				// Delete working area tvShow file.
				LogOutput("Deleting working area file {0}.", workingAreaTVShow.File.Name);
				workingAreaTVShow.File.Delete();

				// If delete original.
				if(deleteOriginal)
				{
					LogOutput("Deleting original file {0}.", tvShow.File.Name);
					tvShow.File.Delete();
				}
			};
		}

		private class WorkingTVShow : ITVShow
		{
			public FileInfoBase File { get; set; }
			public Definition Definition { get; set; }
			public string TVShowName { get; set; }
			public int? SeasonNumber { get; set; }
			public int EpisodeNumber { get; set; }
			public string Name { get; set; }
			public string Description { get; set; }
			public IEnumerable<string> Genres { get; set; }
			public IEnumerable<string> Cast { get; set; }
			public IEnumerable<string> Directors { get; set; }
			public IEnumerable<string> Screenwriters { get; set; }
			public DateTime ReleaseDate { get; set; }
			public string Network { get; set; }
			public IBase64Image Artwork { get; set; }

			public WorkingTVShow(ITVShow tvShow, FileInfoBase file)
			{
				File = file;
				Definition = tvShow.Definition;
				TVShowName = tvShow.TVShowName;
				SeasonNumber = tvShow.SeasonNumber;
				EpisodeNumber = tvShow.EpisodeNumber;
				Name = tvShow.Name;
				Description = tvShow.Description;
				Genres = tvShow.Genres;
				Cast = tvShow.Cast;
				Directors = tvShow.Directors;
				Screenwriters = tvShow.Screenwriters;
				ReleaseDate = tvShow.ReleaseDate;
				Network = tvShow.Network;
				Artwork = tvShow.Artwork;
			}
		}
	}
}

