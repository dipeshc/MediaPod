using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Xml.Serialization.Extensions;
using TvdbLib.Data;
using TvdbLib.Data.Banner;
using System.Xml.Serialization;

namespace TvdbLib.Cache.Extensions
{
	public static class TVDBCacheExtensions
	{
		private class Lock {}
		private static Lock _lock = new Lock();
		private static SearchCache _searchCache;

		public static IEnumerable<int> Search(this TvdbHandler tvdbHandler, IFileSystem fileSystem, DirectoryInfoBase cacheDirectory, string term)
		{
			// Initalise cache if it does not exist.
			if (_searchCache == null)
			{
				_searchCache = new SearchCache(fileSystem, cacheDirectory);
			}

			// If not in cache then search online.
			if(!_searchCache.Contains(term))
			{
				// HACK: TVDB fails on concurrent search, needs a lock to only allow one search.
				lock(_lock)
				{
					// Search.
					var results = tvdbHandler.SearchSeries(term);

					// Return null if no results found.
					if(results.Count==0)
					{
						return null;
					}
					
					// Set cache.
					_searchCache.Insert(term, results.Select(result => result.Id).ToList());
				}
			}

			// Return series id.
			return _searchCache.Get(term);
		}

		public static FileInfoBase GetBannerCacheFile(this TvdbBannerWithThumb banner, IFileSystem fileSystem, DirectoryInfoBase cacheDirectory, bool thumbnail)
		{
			// Initalise empty filename.
			string fileName = "";
			
			// Load Banner if not loaded.
			if(!thumbnail && !banner.IsLoaded)
			{
				banner.LoadBanner();
			}
			else if(thumbnail && !banner.IsThumbLoaded)
			{
				banner.LoadThumb();
			}
			
			// Add pre-fix based on if thumb or not.
			if(thumbnail)
			{
				fileName += "thumb_";
			}
			else
			{
				fileName += "img_";
			}
			
			// Handle different BannerPath conversion for different banner types.
			if(banner.GetType() == typeof(TvdbFanartBanner))
			{
				fileName += "fan_" + fileSystem.FileInfo.FromFileName(banner.BannerPath).Name;
			}
			else
			{
				fileName += banner.BannerPath.Replace("/", "_");
			}
			
			// Return file.
			var firstCombine = fileSystem.Path.Combine(cacheDirectory.FullName, banner.SeriesId.ToString());
			return fileSystem.FileInfo.FromFileName(fileSystem.Path.Combine(firstCombine, fileName));
		}

		public class SearchCache
		{
			private readonly FileInfoBase _searchCacheFile;
			private List<SearchCacheResult> _cache = null;

			public SearchCache(IFileSystem fileSystem, DirectoryInfoBase cacheDirectory)
			{
				_searchCacheFile = fileSystem.FileInfo.FromFileName(fileSystem.Path.Combine(cacheDirectory.FullName, "SearchCache.xml"));
			}

			public class SearchCacheResult
			{
				public string Term;
				public List<int> Ids;
				public DateTime Timestamp;
			}
			
			public bool Contains(string term)
			{
				if(_cache==null)
				{
					Load();
				}
				return _cache.Any(m => m.Term == term);
			}
			
			public List<int> Get(string term)
			{
				if(_cache==null)
				{
					Load();
				}
				return _cache.Find(m => m.Term == term).Ids;
			}
			
			public void Insert(string term, List<int> ids)
			{
				if(Contains(term))
				{
					_cache.Find(m => m.Term == term).Ids = ids;
				}
				else
				{
					_cache.Add(new SearchCacheResult { Term=term, Ids=ids, Timestamp=DateTime.Now });
				}
				Save();
			}
			
			private void Load()
			{
				// If Mappings files does not exist, then create a new mapping.
				if(!_searchCacheFile.Exists)
				{
					_cache = new List<SearchCacheResult>();
					return;
				}
				
				// Read the mappings.
				_cache = _searchCacheFile.XMLDeserializeFile<SearchCacheResult>().ToList();
			}
			
			private void Save()
			{
				// Create the directory if it does not exist.
				if (!_searchCacheFile.Directory.Exists)
				{
					_searchCacheFile.Directory.Create();
				}
				
				// Serialize.
				_cache.XMLSerializeAsSet(_searchCacheFile);
			}
		}
	}
}