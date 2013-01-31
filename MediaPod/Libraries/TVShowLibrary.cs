using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;
using MediaPod.Extractors;
using MediaPod.Interfaces;
using MediaPod.Interfaces.Models;
using MediaPod.Sources;

namespace MediaPod.Libraries
{
	public class TVShowLibrary
	{
		private readonly List<ITVShowMetadataSource> _metadataSources;
		public readonly DirectoryInfoBase RootDirectory;
		public IEnumerable<ITVShowCollection> TVShowCollections { get; private set; }

		public TVShowLibrary (DirectoryInfoBase rootDirectory, IEnumerable<ITVShowMetadataSource> metadataSources)
		{
			RootDirectory = rootDirectory;
			_metadataSources = metadataSources.ToList();
			TVShowCollections = new List<ITVShowCollection>();
		}

		public ITVShowCollection GetTVShowCollectionByName(string tvShowName)
		{
			return TVShowCollections.FirstOrDefault(tvShowCollection => tvShowCollection.Select(tvShow => tvShow.TVShowName).FirstOrDefault() == tvShowName);
		}

		public void Load()
		{
			// Make mapping of tvShowName to tvShowCollections.
			var mappings = new ConcurrentDictionary<string, TVShowCollection>();

			// Get all files with matching extension.
			var files = RootDirectory.GetFiles("*", SearchOption.AllDirectories)
				.Where(file => file.Extension.ToLower().EndsWith(".mp4") || file.Extension.ToLower().EndsWith(".m4v") || file.Extension.ToLower().EndsWith(".mkv"));

			// Loop over each file and extract metadata.
			Parallel.ForEach(files, file =>
			{
				// Extract metadata, if extraction fails, then skip.
				var metadataRegex = new TVShowFilenameRegexExtractor (file.Name);
				if (!metadataRegex.Extract ())
				{
					return;
				}

				// Make new tvShow and set metadata.
				var tvShow = new TVShow(file);
				tvShow.SetMetadata(metadataRegex);

				// Add to mappings.
				if(!mappings.ContainsKey(tvShow.TVShowName))
				{
					mappings [tvShow.TVShowName] = new TVShowCollection (tvShow.TVShowName);
				}
				mappings [tvShow.TVShowName].Add(tvShow);
			});

			// Loop over each mapping and add supplementary metadata where required.
			Parallel.ForEach(mappings, mapping =>
			{
				// Get tvShowName.
				var tvShowName = mapping.Key;

				// Get supplementary metadata as collection.
				var supplementaryMetadataCollection = _metadataSources.First ().Get(tvShowName).OrderBy(sm => sm.SeasonNumber).ThenBy(sm => sm.EpisodeNumber).ToList();

				// Loop over each individual tvShow. Use empty description field to indicate that supplementary metadata required.
				mapping.Value.Where(tvShow => string.IsNullOrEmpty(tvShow.Description)).ToList().ForEach(tvShow =>
				{
					ITVShowMetadata metadata = null;
					if(tvShow.SeasonNumber.HasValue)
					{
						metadata = supplementaryMetadataCollection.FirstOrDefault(sm => sm.SeasonNumber == tvShow.SeasonNumber && sm.EpisodeNumber == tvShow.EpisodeNumber);
					}
					else
					{
						metadata = supplementaryMetadataCollection.Where(sm => sm.SeasonNumber.HasValue && sm.SeasonNumber != 0).ElementAtOrDefault(tvShow.EpisodeNumber);
					}

					if(metadata!=null)
					{
						((TVShow) tvShow).SetMetadata(metadata);
					}
				});
			});

			// Convert mapping to tvShowCollections.
			var tvShowCollections = mappings.Values.OrderBy(tvShowCollection => tvShowCollection.First().TVShowName) .ToList();
			
			// Lock swap.
			lock (TVShowCollections)
			{
				TVShowCollections = tvShowCollections;
			}
		}

		private class TVShowCollection : List<ITVShow>, ITVShowCollection
		{
			public string TVShowName { get; private set; }

			public TVShowCollection(string tvShowName)
			{
				TVShowName = tvShowName;
			}

			public ITVShow GetTVShowBySeasonAndEpisode(int? seasonNumber, int episodeNumber)
			{
				return this.FirstOrDefault(tvShow => tvShow.SeasonNumber == seasonNumber && tvShow.EpisodeNumber == episodeNumber);
			}
		}

		private class TVShow : ITVShow
		{
			public FileInfoBase File { get; private set; }
			public Definition Definition { get; private set; }
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

			public TVShow(FileInfoBase file)
			{
				File = file;
				Definition = MediaPod.Interfaces.Models.Definition.Unknown;
			}

			public void SetMetadata(ITVShowMetadata metadata)
			{
				TVShowName = metadata.TVShowName;
				SeasonNumber = metadata.SeasonNumber;
				EpisodeNumber = metadata.EpisodeNumber;
				Name = metadata.Name;
				Description = metadata.Description;
				Genres = metadata.Genres;
				Cast = metadata.Cast;
				Directors = metadata.Directors;
				Screenwriters = metadata.Screenwriters;
				ReleaseDate = metadata.ReleaseDate;
				Network = metadata.Network;
				Artwork = metadata.Artwork;
			}
		}
	}
}