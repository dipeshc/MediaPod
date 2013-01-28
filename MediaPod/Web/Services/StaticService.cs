using System;
using System.IO;
using System.Net;
using System.Reflection;
using MediaPod.Web.Extensions;
using MediaPod.Web.Routes;
using ServiceStack.Common.ServiceModel;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;

namespace MediaPod.Web.Services
{	
	public class StaticService : BaseService
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
			response.ContentType = resourcePath.ResoveContentType();
			Response.AddHeader("Cache-Control", "public");
			Response.AddHeader("Expires", DateTime.Now.AddYears(1).ToString("ddd, dd MMM yyyy HH:mm:ss 'GMT'"));

			// Return.
			return response;
		}
	}
}