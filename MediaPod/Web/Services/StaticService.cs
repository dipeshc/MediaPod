using System;
using System.IO;
using System.Net;
using System.Reflection;
using MediaPod.Web.Routes;
using ServiceStack.Common.ServiceModel;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;

namespace MediaPod.Web.Services
{	
	[Route("/static/{Path*}")]
	public class StaticRequest : IReturn
	{
		public string Path { get; set; }
	}

	public class StaticService : Service
	{
		public object Get(StaticRequest request)
		{
			// Make static resource path.
			var resourcePath = "MediaPod.Web.Static." + request.Path.Replace ("/", ".");

			// Send back and cache result.
			var response = (CompressedResult) RequestContext.ToOptimizedResultUsingCache<object>(Cache, resourcePath, null, () =>
			{
				// Get resource.
				var stream = Assembly.GetExecutingAssembly ().GetManifestResourceStream (resourcePath);
				
				// Return 404 if not found.
				if (stream == null)
				{
					throw new HttpError(HttpStatusCode.NotFound, "404 Not Found");
				}

				// Return resource.
				using (var reader = new StreamReader(stream))
				{
					return reader.ReadToEnd ();
				}
			});

			// Add headers.
			response.ContentType = MimeTypes.GetMimeType(resourcePath);
			Response.AddHeader("Cache-Control", "public");
			Response.AddHeader("Expires", DateTime.Now.AddYears(1).ToString("ddd, dd MMM yyyy HH:mm:ss 'GMT'"));

			// Return.
			return response;
		}
	}
}