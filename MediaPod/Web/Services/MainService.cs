using RazorEngine;
using RazorEngine.Templating;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using RazorEngine.Templating;

namespace MediaPod.Web.Services
{
	[Route("/MediaPod/{Path*}")]
	public class MainRequest : IReturn
	{
		public string Path { get; set; }
	}

	public class MainService : Service
	{
		public object Get(MainRequest request)
		{
			try
			{
				// Convert path.
				var convertedPath = string.Format("MediaPod.Web.Views.{0}", request.Path.Replace ("/", "."));

				// Get ouput and return optimised.
				var output = Razor.Resolve(convertedPath).Run(new ExecuteContext());
				return RequestContext.ToOptimizedResult(output);
			}
			catch(TemplateParsingException)
			{
				throw;
			}
			catch(TemplateCompilationException)
			{
				throw;
			}
			catch
			{
				throw;
				//throw new HttpError (System.Net.HttpStatusCode.NotFound, "404 Not Found");
			}
		}
	}
}