using System.IO.Abstractions;
using System.Linq;
using System.Text.RegularExpressions;
using MediaPod.Managers;
using ServiceStack.Common;
using ServiceStack.Common.Web;
using ServiceStack.ServiceClient.Web;
using ServiceStack.ServiceHost;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints;

namespace MediaPod.Web.Extensions
{
	public static class RoutingExtensions
	{
		// TODO: Find way to use global fileSystem instead of making a local one.
		private static IFileSystem _fileSystem = new FileSystem();

		public static string ToAbsoluteUri(this IReturn request, string httpMethod = null, string formatFallbackToPredefinedRoute = null, IHttpRequest baseRoutingRequest = null)
		{
			// TODO: Find way to detect protocol (e.g. http OR https).
			var relativeUrl = request.ToUrl(httpMethod ?? HttpMethods.Get, formatFallbackToPredefinedRoute ?? EndpointHost.Config.DefaultContentType.ToContentFormat());
			var absoluteBase = baseRoutingRequest == null ? EndpointHost.Config.WebHostUrl : baseRoutingRequest.UserHostAddress;
			var absoluteUrl = "http://" + absoluteBase.CombineWith(relativeUrl);
			return absoluteUrl;
		}

		public static string ToSiteFilePath(this FileInfoBase file)
		{
			var filePath = file.FullName;
			var tvShowLibraryPath = ResourceManager.TVShowLibrary.RootDirectory.FullName;
			var unorganisedLibraryPath = ResourceManager.UnorganisedLibrary.RootDirectory.FullName;
			
			if (filePath.StartsWith (tvShowLibraryPath))
			{
				var unrootedPath =  new Regex(string.Format(@"{0}{1}?", unorganisedLibraryPath, _fileSystem.Path.DirectorySeparatorChar)).Replace(filePath, "");
				return _fileSystem.Path.Combine("TVShowLibrary/", unrootedPath);
			}
			if (filePath.StartsWith(unorganisedLibraryPath))
			{
				var unrootedPath =  new Regex(string.Format(@"{0}{1}?", unorganisedLibraryPath, _fileSystem.Path.DirectorySeparatorChar)).Replace(filePath, "");
				return _fileSystem.Path.Combine("UnorganisedLibrary/", unrootedPath);
			}

			return filePath;
		}
		
		public static FileInfoBase FromSiteFilePath(this string siteFilePath)
		{
			var siteFilePathRaw = siteFilePath.UrlDecode();
			
			if(siteFilePathRaw.StartsWith("TVShowLibrary/"))
			{
				var manager = ResourceManager.TVShowLibrary;
				var expectedFilePath = manager.RootDirectory.FullName + siteFilePathRaw.Replace("TVShowLibrary/", "");
				return manager.TVShowCollections.SelectMany(tvShowCollection => tvShowCollection).Select(tvShow => tvShow.File).FirstOrDefault(file => file.FullName == expectedFilePath);
			}
			if(siteFilePathRaw.StartsWith("UnorganisedLibrary/"))
			{
				var expectedFilePath = ResourceManager.UnorganisedLibrary.RootDirectory.FullName + siteFilePathRaw.Replace("UnorganisedLibrary/", "");
				return ResourceManager.UnorganisedLibrary.Get().Select(media => media.File).FirstOrDefault(file => file.FullName == expectedFilePath);
			}
			return _fileSystem.FileInfo.FromFileName(siteFilePath);
		}
	}
}

