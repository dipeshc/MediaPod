using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Reflection;
using MediaPod.Interfaces.Models;
using MediaPod.Interfaces;
using MediaPod.Model.Extensions;
using TvdbLib;
using TvdbLib.Cache;
using TvdbLib.Cache.Extensions;
using TvdbLib.Data;
using TvdbLib.Data.Banner;

namespace MediaPod.Sources
{
	public class TVDBTVShowMetadataSource : ITVShowMetadataSource
	{
		private readonly IFileSystem _fileSystem;
		private readonly DirectoryInfoBase _tvdbCacheDirectory;
		private readonly TvdbHandler _tvdbHandler;

		public TVDBTVShowMetadataSource (IFileSystem fileSystem, string apiKey)
		{
			// Set fileSystem.
			_fileSystem = fileSystem;

			// Make cache directory.
			var assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
			_tvdbCacheDirectory = _fileSystem.DirectoryInfo.FromDirectoryName(_fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), assemblyName + _fileSystem.Path.DirectorySeparatorChar +"TVDBCache"));
			if(!_tvdbCacheDirectory.Exists)
			{
				_tvdbCacheDirectory.Create();
			}
			
			// Initalise handler with cache.
			_tvdbHandler = new TvdbHandler(new XmlCacheProvider(_tvdbCacheDirectory.FullName), apiKey);
			if(!_tvdbHandler.IsCacheInitialised)
			{
				_tvdbHandler.InitCache();
			}
		}

		public IEnumerable<ITVShowMetadata> Get(string tvShowName)
		{
			// Search for ids of matching tv show name.
			var ids = _tvdbHandler.Search (_fileSystem, _tvdbCacheDirectory, tvShowName);
			
			// If unable to find matching show.
			if (!ids.Any())
			{
				return null;
			}

			// Get details for first.
			var tvdbSeries = _tvdbHandler.GetSeries(ids.First(), TvdbLanguage.DefaultLanguage, true, false, true);

			// Return ITVShowMetadata.
			return tvdbSeries.Episodes.Select(tvdbEpisode =>
			{
				return new TVShowMetadata(_fileSystem, _tvdbCacheDirectory, tvdbSeries, tvdbEpisode);
			}).ToList();
		}

		public ITVShowMetadata Get(string tvShowName, int? seasonNumber, int episodeNumber)
		{
			return Get(tvShowName).FirstOrDefault(tvShow => tvShow.SeasonNumber == seasonNumber && tvShow.EpisodeNumber == episodeNumber);
		}

		public IEnumerable<ITVShowMetadata> Search(string tvShowName, int? seasonNumber, int episodeNumber)
		{
			// Make results.
			var results = new List<ITVShowMetadata> ();
			
			// Search for ids of matching tv show name.
			List<int> ids;
			try
			{
				// Search.
				ids = _tvdbHandler.Search (_fileSystem, _tvdbCacheDirectory, tvShowName).ToList ();

				// If unable to find matching show.
				if (!ids.Any())
				{
					return results;
				}
			}
			catch (Exception)
			{
				Console.Error.WriteLine("Unable to connect to TVDB.");
			}
			
			// Get details for each result.
			ids.ForEach(id =>
			{
				// Get details.
				var tvdbSeries = _tvdbHandler.GetSeries(id, TvdbLanguage.DefaultLanguage, true, false, true);
				
				// Find the episode and get its details.
				TvdbEpisode tvdbEpisode;
				if(seasonNumber.HasValue)
				{
					tvdbEpisode = tvdbSeries.GetEpisodesAbsoluteOrder().Find(anEpisode => anEpisode.SeasonNumber==seasonNumber && anEpisode.EpisodeNumber==episodeNumber);
				}
				else
				{
					tvdbEpisode = tvdbSeries.GetEpisodes(seasonNumber ?? 0).Find(anEpisode => anEpisode.EpisodeNumber==episodeNumber);
				}
				
				// If no match found, then continue.
				if(tvdbEpisode == null)
				{
					return;
				}
				
				// Create the tvShow and add to list.
				var tvShow = new TVShowMetadata(_fileSystem, _tvdbCacheDirectory, tvdbSeries, tvdbEpisode);
				results.Add(tvShow);
			});
			return results;
		}

		private class TVShowMetadata : ITVShowMetadata
		{
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

			public TVShowMetadata(IFileSystem fileSystem, DirectoryInfoBase cacheDirectory, TvdbSeries tvdbSeries, TvdbEpisode tvdbEpisode)
			{
				// Set metadata.
				TVShowName = tvdbSeries.SeriesName;
				SeasonNumber = tvdbEpisode.SeasonNumber;
				EpisodeNumber = tvdbEpisode.EpisodeNumber;
				Name = tvdbEpisode.EpisodeName;
				Description = tvdbEpisode.Overview;
				Genres = tvdbSeries.Genre;
				Cast = tvdbSeries.Actors;
				Directors = tvdbEpisode.Directors;
				Screenwriters = tvdbEpisode.Writer;
				ReleaseDate = tvdbEpisode.FirstAired;
				Network = tvdbSeries.Network;

				// Get artwork.
				if (SeasonNumber.HasValue && tvdbSeries.SeasonBanners.Any(banner => banner.Season == SeasonNumber))
				{
					Artwork = tvdbSeries.SeasonBanners.Where (banner => banner.Season == SeasonNumber).FirstOrDefault ().GetBannerCacheFile (fileSystem, cacheDirectory, false).ToBase64Image();
				}
				else if(tvdbSeries.PosterBanners.Any())
				{
					Artwork = tvdbSeries.PosterBanners.FirstOrDefault ().GetBannerCacheFile (fileSystem, cacheDirectory, false).ToBase64Image();
				}
				else if(tvdbSeries.SeriesBanners.Any())
				{
					Artwork = tvdbSeries.SeriesBanners.FirstOrDefault ().GetBannerCacheFile (fileSystem, cacheDirectory, false).ToBase64Image();
				}
			}
		}
	}
}