using System.IO;
using System.Reflection;
using Funq;
using RazorEngine;
using RazorEngine.Configuration;
using RazorEngine.Templating;
using ServiceStack.Common;
using ServiceStack.CacheAccess;
using ServiceStack.CacheAccess.Providers;
using ServiceStack.ServiceInterface;
using ServiceStack.WebHost.Endpoints;

namespace MediaPod.Web
{
	public class AppHost : AppHostHttpListenerBase 
	{
		public AppHost() : base("MediaPod", typeof(AppHost).Assembly) { }
		
		public override void Configure(Container container)
		{
			// Enable caching.
			container.Register<ICacheClient>(new MemoryCacheClient());

			// Setup razor.
			string viewPathTemplate = "MediaPod.Web.Views.{0}";
			TemplateServiceConfiguration templateConfig = new TemplateServiceConfiguration();
			templateConfig.Resolver = new DelegateTemplateResolver(name =>
			                                                       {
				string resourcePath = string.Format(viewPathTemplate, name);
				if(!resourcePath.EndsWith(".cshtml"))
				{
					resourcePath += ".cshtml";
				}
				var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourcePath);
				using (StreamReader reader = new StreamReader(stream))
				{
					return reader.ReadToEnd();
				}
			});
			Razor.SetTemplateService(new TemplateService(templateConfig));
		}
	}
}