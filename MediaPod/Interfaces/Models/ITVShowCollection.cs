using System.Collections.Generic;

namespace MediaPod.Interfaces.Models
{
	public interface ITVShowCollection : IEnumerable<ITVShow>
	{
		ITVShow GetTVShowBySeasonAndEpisode(int? seasonNumber, int episodeNumber);
	}
}