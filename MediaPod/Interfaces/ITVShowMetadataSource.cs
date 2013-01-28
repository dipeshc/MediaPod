using System.Collections.Generic;
using MediaPod.Interfaces.Models;

namespace MediaPod.Interfaces
{
	public interface ITVShowMetadataSource
	{
		IEnumerable<ITVShowMetadata> Get(string tvShowName);
		ITVShowMetadata Get(string tvShowName, int? seasonNumber, int episodeNumber);
		IEnumerable<ITVShowMetadata> Search(string tvShowName, int? seasonNumber, int episodeNumber);
	}
}