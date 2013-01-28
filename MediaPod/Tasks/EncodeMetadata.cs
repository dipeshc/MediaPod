using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Reflection;
using MediaPod.Externals;
using MediaPod.Interfaces.Models;
using MediaPod.Model.Extensions;

namespace MediaPod.Tasks
{
	public class EncodeMetadata : BaseTask
	{
		private static int idCounter = 0;

		public EncodeMetadata (IFileSystem fileSystem, ITVShow tvShow, TextWriter stdOut=null, TextWriter stdErr=null) : base("EncodeMetadata" + idCounter++, stdOut, stdErr)
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

				// Make temp encoding file and artwork file.
				var workingAreaEncodingFile = fileSystem.FileInfo.FromFileName(fileSystem.Path.Combine(workingArea.FullName, tvShow.File.Name));
				var tempArtworkFile = tvShow.Artwork.ToFile(fileSystem);

				// Setup default arguments.
				var arguments = string.Format ("-source \"{0}\" -dest \"{1}\" ", tvShow.File.FullName, workingAreaEncodingFile.FullName);
				
				// Set metadata arguments.
				var metadataList = new Dictionary<string, string> ();
				metadataList ["Name"] = tvShow.Name;
				metadataList ["Artist"] = tvShow.TVShowName;
				metadataList ["Album"] = tvShow.TVShowName;
				metadataList ["Genre"] = string.Join (", ", tvShow.Genres);
				metadataList ["Release Date"] = tvShow.ReleaseDate.ToString ("yyyy-MM-dd");
				metadataList ["Track #"] = string.Format ("{0}", tvShow.EpisodeNumber);
				metadataList ["TV Show"] = tvShow.TVShowName.ToString ();
				metadataList ["TV Episode #"] = tvShow.EpisodeNumber.ToString ();
				metadataList ["TV Network"] = tvShow.Network;
				metadataList ["TV Episode ID"] = string.Format("{0}{1:D2}", tvShow.SeasonNumber, tvShow.EpisodeNumber);
				metadataList ["TV Season"] = tvShow.SeasonNumber.ToString ();
				metadataList ["Description"] = tvShow.Description;
				metadataList ["Long Description"] = tvShow.Description;
				metadataList ["Cast"] = string.Join (", ", tvShow.Cast);
				metadataList ["Director"] = string.Join (", ", tvShow.Directors);
				metadataList ["Screenwriters"] = string.Join (", ", tvShow.Screenwriters);
				metadataList ["HD Video"] = ((int) tvShow.Definition).ToString();
				metadataList ["Media Kind"] = "TV Show";
				metadataList ["Artwork"] = tempArtworkFile.FullName;
				
				// Convert metadata arguments list to string.
				if (metadataList.Any ())
				{
					arguments += " -metadata ";
					foreach (var metadata in metadataList)
					{
						arguments += string.Format ("\"{{{0}:{1}}}\"", metadata.Key, metadata.Value);
					}
				}
				
				// Encode.
				LogOutput("Encoding {0} to working area location {1}.", tvShow.File.FullName, workingAreaEncodingFile.FullName);
				if(External.Run(External.Application.SublerCLI, arguments, StdOut, StdErr).ExitCode!=0)
				{
					LogError("Encoding failed.");
					LogOutput("Deleting temp artwork file {0}.", tempArtworkFile.FullName);
					return;
				}

				// Delete temp artwork file.
				LogOutput("Deleting temp artwork file {0}.", tempArtworkFile.FullName);
				tempArtworkFile.Delete();

				// Replace original file with newly created encoded file.
				LogOutput("Deleting original file {0}.", tvShow.File.FullName);
				tvShow.File.Delete();

				LogOutput("Moving newly created encoded file {0} to orginal file location {1}.", workingAreaEncodingFile.FullName, tvShow.File.FullName);
				workingAreaEncodingFile.MoveTo(tvShow.File.FullName);
			};
		}
	}
}