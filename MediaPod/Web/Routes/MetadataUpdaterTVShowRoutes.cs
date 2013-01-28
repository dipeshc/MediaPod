using System;
using ServiceStack.ServiceHost;

namespace MediaPod.Web.Routes
{
	[Route("/MetadataUpdater/TVShow/{Path*}", "GET")]
	[Route("/MetadataUpdater/TVShow/{Path}", "GET")]
	public class MetadataUpdaterTVShowRequest : IReturn
	{
		public string Path { get; set; }
	}

	[Route("/MetadataUpdater/TVShow/{Path*}", "POST")]
	[Route("/MetadataUpdater/TVShow/{Path}", "POST")]
	public class MetadataUpdaterTVShowUpdateRequest : IReturn
	{
		public string Path { get; set; }
		public string TVShowName { get; set; }
		public int? SeasonNumber { get; set; }
		public int EpisodeNumber { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }
		public string Genres { get; set; }
		public string Cast { get; set; }
		public string Directors { get; set; }
		public string Screenwriters { get; set; }
		public string ReleaseDate { get; set; }
		public string Network { get; set; }
		public string Artwork { get; set; }
	}

	[Route("/MetaDataUpdater/TVShow/Search")]
	public class MetadataUpdaterTVShowSearchRequest : IReturn
	{
		public string TVShowName { get; set; }
		public int? SeasonNumber { get; set; }
		public int? EpisodeNumber { get; set; }
	}
}