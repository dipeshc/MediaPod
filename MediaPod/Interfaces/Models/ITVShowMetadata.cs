using System;
using System.Collections.Generic;
using System.IO.Abstractions;

namespace MediaPod.Interfaces.Models
{
	public enum Base64ImageType
	{
		Unknown,
		PNG,
		JPEG,
		GIF
	}

	public interface IBase64Image
	{
		Base64ImageType Type { get; }
		string Data { get; }
	}

	public interface ITVShowMetadata
	{
		string TVShowName { get; }
		int? SeasonNumber { get; }
		int EpisodeNumber { get; }
		string Name { get; }
		string Description { get; }
		IEnumerable<string> Genres { get; }
		IEnumerable<string> Cast { get; }
		IEnumerable<string> Directors { get; }
		IEnumerable<string> Screenwriters { get; }
		DateTime ReleaseDate { get; }
		string Network { get; }
		IBase64Image Artwork { get; }
	}
}