using System;
using MediaPod.Web;

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
	}
}