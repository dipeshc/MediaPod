using System.Collections.Generic;

namespace MediaPod.Interfaces.Models
{
	public interface ITVShowCollection : IEnumerable<ITVShow>
	{
		string TVShowName { get; }
		ITVShow GetTVShowBySeasonAndEpisode(int? seasonNumber, int episodeNumber);
	}
}