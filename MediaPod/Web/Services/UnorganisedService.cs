using MediaPod.Managers;
using MediaPod.Web.Routes;

namespace MediaPod.Web.Services
{
	public class UnorganisedService : BaseService
	{
		public object Get(UnorganisedRequest request)
		{	
			// Get unorganised and display.
			return RenderViewOptimized ("Unorganised.Index", new { UnorganisedMedia = ResourceManager.UnorganisedLibrary.Get() });
		}
	}
}