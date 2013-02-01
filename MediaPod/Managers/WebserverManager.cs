using System.IO;
using System.Reflection;
using Funq;
using RazorEngine;
using RazorEngine.Configuration;
using RazorEngine.Templating;
using ServiceStack.CacheAccess;
using ServiceStack.CacheAccess.Providers;
using ServiceStack.WebHost.Endpoints;

namespace MediaPod.Managers
{
	public class WebserverManager
	{
		private int _port;

		public WebserverManager(int port)
		{
			_port = port;
		}

		public void Run()
		{
			var appHost = new AppHost();
			appHost.Init();
			appHost.Start(string.Format("http://*:{0}/", _port));
		}

		private class AppHost : AppHostHttpListenerBase 
		{
			public AppHost() : base("MediaPod", typeof(AppHost).Assembly) { }
			
			public override void Configure(Container container)
			{
				// Enable caching.
				container.Register<ICacheClient>(new MemoryCacheClient());
				
				// Setup razor.
				TemplateServiceConfiguration templateConfig = new TemplateServiceConfiguration();
				templateConfig.Resolver = new DelegateTemplateResolver(name =>
				{
					var resourcePath = name;
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
}