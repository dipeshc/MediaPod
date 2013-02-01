using System.IO.Abstractions;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using MediaPod.Api.Extensions;
using MediaPod.Managers;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceClient.Web;
using ServiceStack.Text;

namespace MediaPod.Api.Services
{
	[Route("/Api/TVShows")]
	[Route("/Api/TVShows/{TVShowName}")]
	public class TVShowRequest : IReturn
	{
		public string TVShowName { get; set; }
	}
	
	[Route("/Api/TVShows/{TVShowName}/Seasons/{SeasonNumber}/Episodes/{EpisodeNumber}/File/{FileName}")]
	[Route("/Api/TVShows/{TVShowName}/Episodes/{EpisodeNumber}/File/{FileName}")]
	public class TVShowFileRequest : IReturn
	{
		public string TVShowName { get; set; }
		public int? SeasonNumber { get; set; }
		public int EpisodeNumber { get; set; }
		public string FileName { get; set; }
	}
	
	[Route("/Api/TVShows/{TVShowName}/Artwork.png")]
	public class TVShowArtworkRequest : IReturn
	{
		public string TVShowName { get; set; }
	}

	public class TVShowService : Service
	{
		public object Get(TVShowRequest request)
		{
			// If TVShowName is NOT provided then return link to each collection.
			if (string.IsNullOrEmpty (request.TVShowName))
			{
				return ResourceManager.TVShowLibrary.TVShowCollections.Select (tvShowCollection =>
				{
					var tvShowName = tvShowCollection.TVShowName;
					var url = new TVShowRequest () { TVShowName = tvShowName }.ToUrl ("GET");
					return new
					{
						TVShowName = tvShowName,
						Url = url
					};
				});
			}

			// If TVShowName provided, then get matching collection.
			var cleanTVShowName = request.TVShowName.UrlDecode ();
			return ResourceManager.TVShowLibrary.GetTVShowCollectionByName (cleanTVShowName).Select(tvShow =>
			{
				var fileUrl = new TVShowFileRequest()
				{
					TVShowName = tvShow.TVShowName,
					SeasonNumber = tvShow.SeasonNumber,
					EpisodeNumber = tvShow.EpisodeNumber,
					FileName = tvShow.File.Name
				}.ToUrl ("GET");
				var artworkUrl = new TVShowArtworkRequest()
				{
					TVShowName = tvShow.TVShowName
				}.ToUrl("GET");
				return new
				{
					File = fileUrl,
					Definition = tvShow.Definition,
					TVShowName = tvShow.TVShowName,
					SeasonNumber = tvShow.SeasonNumber,
					EpisodeNumber = tvShow.EpisodeNumber,
					Name =  tvShow.Name,
					Description = tvShow.Description,
					Genres = tvShow.Genres,
					Cast = tvShow.Cast,
					Directors = tvShow.Directors,
					Screenwriters = tvShow.Screenwriters,
					ReleaseDate = tvShow.ReleaseDate,
					Network = tvShow.Network,
					Artwork = artworkUrl
				};
			});
		}

		public object Get(TVShowFileRequest request)
		{
			// Get the TVShowCollection.
			var cleanTVShowName = request.TVShowName.UrlDecode ();
			var tvShowCollection = ResourceManager.TVShowLibrary.GetTVShowCollectionByName (cleanTVShowName);
			
			// Check exists.
			if (tvShowCollection == null)
			{
				throw new HttpError (HttpStatusCode.NotFound, "404 Not Found");
			}
			
			// Get specifiec tv show.
			var tvShow = tvShowCollection.GetTVShowBySeasonAndEpisode (request.SeasonNumber, request.EpisodeNumber);
			
			// Check exists.
			if (tvShow == null || tvShow.File.Name!=request.FileName.UrlDecode())
			{
				throw new HttpError (HttpStatusCode.NotFound, "404 Not Found");
			}
			
			// Transmit tv show.
			StreamFile(tvShow.File);
			return null;
		}
		
		private void StreamFile(FileInfoBase file)
		{
			// Set varaibles.
			const int bufferReadSize = 1024 * 8;
			var fileSize = file.Length;
			long start = 0;
			long end = fileSize - 1;
			
			// Check if range specified.
			if(Request.Headers.AllKeys.Contains("Range"))
			{
				// Extract range details.
				var range = Request.Headers.Get("Range");
				var rangeRegex = new Regex(@"^bytes=(\d+)-(\d*)", RegexOptions.IgnoreCase);
				var match = rangeRegex.Match(range);
				
				// Extract start.
				if(match.Groups.Count>1 && !string.IsNullOrEmpty(match.Groups[1].Value))
				{
					long.TryParse(match.Groups[1].Value, out start);
				}
				
				// Extract end.
				if(match.Groups.Count>2 && !string.IsNullOrEmpty(match.Groups[2].Value))
				{
					long.TryParse(match.Groups[2].Value, out end);
				}
				
				// Check if Offset is valid.
				if(start < 0 || start > end)
				{
					Response.StatusCode = 416;
					Response.StatusDescription = "Requested Range Not Satisfiable";
					Response.End();
					return;
				}
			}
			
			if(start != 0)
			{
				Response.StatusCode = 206;
				Response.StatusDescription = "Partial Content";
			}
			
			// Set headers.
			Response.ContentType = MimeTypes.GetMimeType(file.Name);
			Response.AddHeader("Accept-Ranges", "bytes");
			Response.AddHeader("Content-Range", string.Format("bytes {0}-{1}/{2}", start, end, fileSize));
			Response.AddHeader("Content-Length", (end + 1 - start).ToString());
			
			// Transmit the file.
			Response.OutputStream.TransmitFile(file, start, (end + 1 - start));
			Response.End();
		}
	}
}