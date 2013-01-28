using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Text.RegularExpressions;
using MediaPod.Interfaces.Models;

namespace MediaPod.Extractors
{
	public class TVShowFilenameRegexExtractor : ITVShowMetadata
	{
		private static string _tvShowNamePatternPreFix = @"\[\w*\][\W_]?";
		private static string _tvShowNamePatternShowName = @"(?<TVShowName>.+)";
		private static string _tvShowNamePatternSeasonEpisode = @"[Ss](?<SeasonNumber>\d+)[Ee](?<EpisodeNumber>\d+)";
		private static string _tvShowNamePatternSeasonEpisodeCross = @"(?<SeasonNumber>\d+)x(?<EpisodeNumber>\d{2,})";
		private static string _tvShowNamePatternEpisodeOnly = @"E(?<EpisodeNumber>\d{2,})";
		private static string _tvShowNamePatternEpisodeNumberOnly = @"(?<EpisodeNumber>\d{3,})";
		
		private static IEnumerable<Regex> _patterns = null;
		private static IEnumerable<Regex> patterns
		{
			get
			{
				if(_patterns==null)
				{
					var tvShowNamePattern1 = new Regex(string.Format(@"^(?:{0})?{1}(?=[\W_]{2})", _tvShowNamePatternPreFix, _tvShowNamePatternShowName, _tvShowNamePatternSeasonEpisode));
					var tvShowNamePattern2 = new Regex(string.Format(@"^(?:{0})?{1}(?=[\W_]{2})", _tvShowNamePatternPreFix, _tvShowNamePatternShowName, _tvShowNamePatternSeasonEpisodeCross));
					var tvShowNamePattern3 = new Regex(string.Format(@"^(?:{0})?{1}(?=[\W_]{2})", _tvShowNamePatternPreFix, _tvShowNamePatternShowName, _tvShowNamePatternEpisodeOnly));
					var tvShowNamePattern4 = new Regex(string.Format(@"^(?:{0})?{1}(?=[\W_]{2})", _tvShowNamePatternPreFix, _tvShowNamePatternShowName, _tvShowNamePatternEpisodeNumberOnly));
					_patterns = new List<Regex>() { tvShowNamePattern1, tvShowNamePattern2, tvShowNamePattern3, tvShowNamePattern4 };
				}
				return _patterns;
			}
		}
		
		private readonly string _filename;

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
		
		public TVShowFilenameRegexExtractor (string filename)
		{
			_filename = filename;
		}
		
		public bool Extract()
		{
			// Use regex to extract out details.
			foreach(var Pattern in patterns)
			{
				// Use regex to extract out details.
				var Match = Pattern.Match(_filename);
				if(!Match.Success)
				{
					continue;
				}
				
				// Set the details.
				TVShowName = Regex.Replace(Match.Groups["TVShowName"].Value, @"[.-]+", " ").Trim();
				
				if(!string.IsNullOrEmpty(Match.Groups["SeasonNumber"].Value))
				{
					SeasonNumber = Int32.Parse(Match.Groups["SeasonNumber"].Value);
				}
				EpisodeNumber = Int32.Parse(Match.Groups["EpisodeNumber"].Value);

				return true;
			}

			// Return false, unable to find match.
			return false;
		}
	}
}