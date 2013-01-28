using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Abstractions;
using System.Linq;
using MediaPod.Externals;
using MediaPod.Interfaces.Models;
using MediaPod.Model.Extensions;

namespace MediaPod.Extractors
{
	public class TVShowMpegFileMetadataExtractor : ITVShowMetadata
	{
		private IFileSystem _fileSystem;

		public FileInfoBase File { get; private set; }
		public string TVShowName { get; private set; }
		public int? SeasonNumber { get; private set; }
		public int EpisodeNumber { get; private set; }
		public string Name { get; private set; }
		public string Description { get; private set; }
		public IEnumerable<string> Genres { get; private set; }
		public IEnumerable<string> Cast { get; private set; }
		public IEnumerable<string> Directors { get; private set; }
		public IEnumerable<string> Screenwriters { get; private set; }
		public DateTime ReleaseDate { get; private set; }
		public string Network { get; private set; }
		public IBase64Image Artwork { get; private set; }

		public TVShowMpegFileMetadataExtractor (IFileSystem fileSystem, FileInfoBase file)
		{
			_fileSystem = fileSystem;
			File = file;
		}

		public bool Extract()
		{
			// Extract metadata.
			var arguments = string.Format ("-source \"{0}\" -listmetadata", File.FullName);
			var output = External.Run (External.Application.SublerCLI, arguments, Console.Out, Console.Error);

			// Map raw metadata into dictionary.
			var mappings = new Dictionary<string, string> ();
			foreach (var line in output.StdOut.Split('\n'))
			{
				var parts = line.Split (':');
				if(parts.Count() > 1)
				{
					mappings.Add (parts [0].Trim (), parts [1].Trim ());
				}
			}

			// Check if atleast minimum requirements met.
			int tempInt;
			if (string.IsNullOrEmpty (TryRetrieveData(mappings, "TV Show")) || !int.TryParse (TryRetrieveData(mappings, "TV Episode #"), out tempInt))
			{
				return false;
			}

			// Set values.
			TVShowName = TryRetrieveData(mappings, "TV Show");
			int tempSeasonNumber;
			if (int.TryParse (TryRetrieveData(mappings, "TV Season"), out tempSeasonNumber))
			{
				SeasonNumber = tempSeasonNumber;
			}
			EpisodeNumber = int.Parse (mappings ["TV Episode #"]);
			Name = TryRetrieveData(mappings, "Name");
			Description = TryRetrieveData(mappings, "Long Description");
			Genres = TryRetrieveData(mappings, "Genre").Split (',').Select (genre => genre.Trim ()).ToList ();
			Cast = TryRetrieveData(mappings, "Cast").Split (',').Select (cast => cast.Trim ()).ToList ();
			Directors = TryRetrieveData(mappings, "Director").Split (',').Select (director => director.Trim ()).ToList ();
			Screenwriters = TryRetrieveData(mappings, "Screenwriters").Split (',').Select (screenwriter => screenwriter.Trim ()).ToList ();
			DateTime tempReleaseDate;
			if (DateTime.TryParseExact (mappings ["Release Date"], "yyyy-MM-dd", null, DateTimeStyles.None, out tempReleaseDate))
			{
				ReleaseDate = tempReleaseDate;
			}
			Network = TryRetrieveData(mappings, "TV Network");

			// Extract artwork.
			var artworkStream = new MediaStreamsExtractor (File).Extract ().LastOrDefault (stream => stream.Codec == MediaStreamsExtractor.CodecType.PNG
			                                                                               || stream.Codec == MediaStreamsExtractor.CodecType.JPEG);
			if (artworkStream != null)
			{
				var tempfile = _fileSystem.FileInfo.FromFileName (_fileSystem.Path.GetTempFileName () + ".png");
				var ffmepgArguments = string.Format ("-i \"{0}\" -map 0:{1} \"{2}\"", File.FullName, artworkStream.Id, tempfile.FullName);
				if (External.Run (External.Application.FFmpeg, ffmepgArguments, Console.Out, Console.Error).ExitCode == 0)
				{
					Artwork = tempfile.ToBase64Image ();
				}
				tempfile.Delete ();
			}

			// Return true.
			return true;
		}

		private static string TryRetrieveData(Dictionary<string, string> mappings, string key)
		{
			if (mappings.ContainsKey (key))
			{
				return mappings[key];
			}
			return string.Empty;
		}
	}
}