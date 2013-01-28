using RazorEngine;
using RazorEngine.Templating;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;

namespace MediaPod.Web.Services
{	
	public class BaseService : Service
	{
		public string RenderView(string viewPath)
		{
			return RenderView(viewPath, new { });
		}

		public string RenderView(string viewPath, object model)
		{
			return Razor.Resolve(viewPath, model).Run(new ExecuteContext());
		}

		public CompressedResult RenderViewOptimized(string viewPath)
		{
			return (CompressedResult) RequestContext.ToOptimizedResult(RenderView(viewPath));
		}

		public CompressedResult RenderViewOptimized(string viewPath, object model)
		{
			return (CompressedResult) RequestContext.ToOptimizedResult(RenderView(viewPath, model));
		}
	}
}