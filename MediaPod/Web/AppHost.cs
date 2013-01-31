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
			// Enable debug.
			SetConfig(new EndpointHostConfig { DebugMode = true, WriteErrorsToResponse=true });

			// Enable caching.
			container.Register<ICacheClient>(new MemoryCacheClient());

			// Setup razor.
			TemplateServiceConfiguration templateConfig = new TemplateServiceConfiguration();
			templateConfig.Resolver = new DelegateTemplateResolver(name =>
			{
				var resourcePath = "";
				if(!name.StartsWith("MediaPod."))
				{
					resourcePath = string.Format("MediaPod.Web.Views.{0}", name);
				}
				else
				{
					resourcePath = name;
				}

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

			// Register custom formats.
			MediaPod.Api.CustomFormats.PodcastFormat.Register(this);
		}
	}
}