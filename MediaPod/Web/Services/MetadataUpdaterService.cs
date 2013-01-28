using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Globalization;
using MediaPod.Extractors;
using MediaPod.Interfaces.Models;
using MediaPod.Managers;
using MediaPod.Model.Extensions;
using MediaPod.Tasks;
using MediaPod.Web.Routes;
using MediaPod.Web.Extensions;
using ServiceStack.Text;

namespace MediaPod.Web.Services
{
	public class MetadataUpdaterService : BaseService
	{
		private static readonly IFileSystem _fileSystem = new FileSystem();

		public object Get(MetadataUpdaterTVShowRequest request)
		{
			var path = request.Path.UrlDecode();
			return RenderViewOptimized ("MetadataUpdater.TVShow", new { File = _fileSystem.FileInfo.FromFileName(path) });
		}

		public object Get(MetadataUpdaterTVShowSearchRequest request)
		{
			var results = ResourceManager.MetadataSource.Search(request.TVShowName, request.SeasonNumber, request.EpisodeNumber.Value);
			return RenderViewOptimized ("MetadataUpdater.TVShowSearchResults", new { Results = results });
		}

		public object Post(MetadataUpdaterTVShowUpdateRequest request)
		{
			// Convert the request to a tvshow.
			var tvShow = new MetadataUpdaterTVShow(request);

			// Create task.
			ResourceManager.QueuedTaskManager.Enqueue(new RemuxEncodeMetadataAndAddToLibrary(new FileSystem(), tvShow, false));

			// Return null.
			return null;
		}

		private class MetadataUpdaterTVShow : ITVShow
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

			public MetadataUpdaterTVShow(MetadataUpdaterTVShowUpdateRequest request)
			{
				var path = request.Path.UrlDecode();
				File = _fileSystem.FileInfo.FromFileName(path);
				Definition = MediaPod.Interfaces.Models.Definition.Unknown;
				TVShowName = request.TVShowName;
				SeasonNumber = Convert.ToInt32(request.SeasonNumber);
				EpisodeNumber = Convert.ToInt32(request.EpisodeNumber);
				Name = request.Name;
				Description = request.Description;
				Genres = request.Genres.Split(',').Select(genre => genre.Trim()).ToList();
				Cast = request.Cast.Split(',').Select(cast => cast.Trim()).ToList();
				Directors = request.Directors.Split(',').Select(director => director.Trim()).ToList();
				Screenwriters = request.Screenwriters.Split(',').Select(screenwriters => screenwriters.Trim()).ToList();
				ReleaseDate = DateTime.ParseExact(request.ReleaseDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);
				Network = request.Network;
				Artwork = request.Artwork.FromWebImage();

				// Determine definition.
				var streams = new MediaStreamsExtractor(File).Extract();
				var height = streams.Where(stream => stream is MediaStreamsExtractor.IVideoStream).Select(stream => ((MediaStreamsExtractor.IVideoStream) stream).Height).OrderByDescending(h => h).First();
				if(height >= 1080)
				{
					Definition = Definition.HD1080;
				}
				else if(height >= 720)
				{
					Definition = Definition.HD720;
				}
				else
				{
					Definition = Definition.SD;
				}
			}
		}
	}
}