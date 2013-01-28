using ServiceStack.ServiceHost;

namespace MediaPod.Web.Routes
{
	[Route("/static/{Path*}")]
	public class StaticRequest : IReturn
	{
		public string Path { get; set; }
	}
}