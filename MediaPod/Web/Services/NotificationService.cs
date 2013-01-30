using System.Linq;
using MediaPod.Managers;
using MediaPod.Web.Routes;

namespace MediaPod.Web.Services
{
	public class NotificationService : BaseService
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