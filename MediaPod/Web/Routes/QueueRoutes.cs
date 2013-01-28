using ServiceStack.ServiceHost;

namespace MediaPod.Web.Routes
{
	[Route("/Queue/Executing", "GET")]
	public class ExecutingRequest : IReturn
	{
	}

	[Route("/Queue/Queued", "GET")]
	public class QueuedRequest : IReturn
	{
	}
}