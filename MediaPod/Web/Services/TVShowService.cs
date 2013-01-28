using System.IO.Abstractions;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using MediaPod.Managers;
using MediaPod.Web.Extensions;
using MediaPod.Web.Routes;
using ServiceStack.Common.Web;
using ServiceStack.Text;

namespace MediaPod.Web.Services
{
	public class TVShowService : BaseService
	{
		public object Get(TVShowRequest request)
		{
			// If TVShowName is NOT provided then show index.
			if (string.IsNullOrEmpty (request.TVShowName))
			{
				var tvShowCollections = ResourceManager.TVShowLibrary.TVShowCollections;
				return RenderViewOptimized ("TVShows.Index", new { TVShowCollections = tvShowCollections });
			}

			// Get the TVShowCollection.
			var cleanTVShowName = request.TVShowName.UrlDecode ();
			var tvShowCollection = ResourceManager.TVShowLibrary.GetTVShowCollectionByName (cleanTVShowName);

			// Check exists.
			if (tvShowCollection == null)
			{
				throw new HttpError (HttpStatusCode.NotFound, "404 Not Found");
			}

			// Show details.
			return RenderViewOptimized ("TVShows.Details", new { TVShowCollection = tvShowCollection });
		}

		public object Get(TVShowPodcastRequest request)
		{
			// Get the TVShowCollection.
			var cleanTVShowName = request.TVShowName.UrlDecode ();
			var tvShowCollection = ResourceManager.TVShowLibrary.GetTVShowCollectionByName (cleanTVShowName);
			
			// Check exists.
			if (tvShowCollection == null)
			{
				throw new HttpError (System.Net.HttpStatusCode.NotFound, "404 Not Found");
			}

			var response = RenderViewOptimized("TVShows.Podcast", new { CurrentRequest = Request, TVShowCollection = tvShowCollection });
			response.ContentType = "application/xml";
			return response;
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
			Response.ContentType = file.Name.ResoveContentType();
			Response.AddHeader("Accept-Ranges", "bytes");
			Response.AddHeader("Content-Range", string.Format("bytes {0}-{1}/{2}", start, end, fileSize));
			Response.AddHeader("Content-Length", (end + 1 - start).ToString());

			// Transmit the file.
			Response.OutputStream.TransmitFile(file, start, (end + 1 - start));
			Response.End();
		}
	}
}