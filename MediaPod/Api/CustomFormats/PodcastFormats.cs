using System;
using System.IO;
using MediaPod.Interfaces.Models;
using RazorEngine;
using RazorEngine.Templating;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints;

namespace MediaPod.Api.CustomFormats
{
	public class PodcastFormat
	{
		private const string _podcastFormat = "text/podcast";
		
		public static void Register(IAppHost appHost)
		{
			appHost.ContentTypeFilters.Register(_podcastFormat, SerializeToStream, DeserializeFromStream);

			appHost.ResponseFilters.Add((request, response, dto) =>
			{
				if (request.ResponseContentType == _podcastFormat)
				{
					response.AddHeader(HttpHeaders.ContentType, "application/xml");
				}
			});
		}
		
		public static void SerializeToStream(IRequestContext requestContext, object response, Stream stream)
		{
			var tvShowCollectionResponse = response as ITVShowCollection;
			using (var streamWriter = new StreamWriter(stream))
			{
				if (tvShowCollectionResponse != null)
				{
					return;
				}
				var model = new { CurrentRequest = requestContext, TVShowCollection = tvShowCollectionResponse };
				try
				{
					var output = Razor.Resolve("MediaPod.Api.CustomFormats.TVShowPodcast", model).Run(new ExecuteContext());
					streamWriter.Write(output);
				}
				catch(Exception e)
				{
					streamWriter.Write(e);
				}
			}
		}
		
		public static object DeserializeFromStream(Type type, Stream stream)
		{
			throw new NotImplementedException();
		}
	}
}