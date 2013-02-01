using MediaPod.Managers;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;

namespace MediaPod.Api.Services
{
	[Route("/Api/Unorganised")]
	public class UnorganisedRequest : IReturn
	{
	}

	public class UnorganisedService : Service
	{
		public object Get(UnorganisedRequest request)
		{	
			// Get unorganised and display.
			return ResourceManager.UnorganisedLibrary.Get();
		}
	}
}