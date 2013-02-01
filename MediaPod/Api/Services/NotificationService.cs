using System.Linq;
using MediaPod.Managers;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;

namespace MediaPod.Api.Services
{
	[Route("/Api/Notifications", "GET")]
	public class NotificationsRequest : IReturn
	{
	}

	public class NotificationService : Service
	{
		public object Get(NotificationsRequest request)
		{
			// Get notifications.
			var notifications = ResourceManager.NotificationManager.Notifications;

			// Make as seen.
			notifications.ToList().ForEach(notification => notification.Seen());

			// Return notifications.
			return notifications;
		}
	}
}