using System.IO.Abstractions;
using System.Linq;
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
		public static string ToAbsoluteUri(this IReturn request, string httpMethod = null, string formatFallbackToPredefinedRoute = null, IHttpRequest baseRoutingRequest = null)
		{
			// TODO: Find way to detect protocol (e.g. http OR https).
			var relativeUrl = request.ToUrl(httpMethod ?? HttpMethods.Get, formatFallbackToPredefinedRoute ?? EndpointHost.Config.DefaultContentType.ToContentFormat());
			var absoluteBase = baseRoutingRequest == null ? EndpointHost.Config.WebHostUrl : baseRoutingRequest.UserHostAddress;
			var absoluteUrl = "http://" + absoluteBase.CombineWith(relativeUrl);
			return absoluteUrl;
		}
	}
}

