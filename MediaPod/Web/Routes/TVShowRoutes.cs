using ServiceStack.ServiceHost;

namespace MediaPod.Web.Routes
{
	[Route("/TVShows")]
	[Route("/TVShows/{TVShowName}")]
	public class TVShowRequest : IReturn
	{
		public string TVShowName { get; set; }
	}
	
	[Route("/TVShows/{TVShowName}/Podcast")]
	public class TVShowPodcastRequest: IReturn
	{
		public string TVShowName { get; set; }
	}
	
	[Route("/TVShows/{TVShowName}/Seasons/{SeasonNumber}/Episodes/{EpisodeNumber}/File/{FileName}")]
	[Route("/TVShows/{TVShowName}/Episodes/{EpisodeNumber}/File/{FileName}")]
	public class TVShowFileRequest : IReturn
	{
		public string TVShowName { get; set; }
		public int? SeasonNumber { get; set; }
		public int EpisodeNumber { get; set; }
		public string FileName { get; set; }
	}
}