using System;
using System.Collections.Generic;
using System.Linq;
using MediaPod.Interfaces;

namespace MediaPod.Managers
{
	public class NotificationManager
	{
		private List<INotification> _notifications;
		public IEnumerable<INotification> Notifications
		{
			get
			{
				Maintenance();
				return _notifications;
			}
		}

		public NotificationManager()
		{
			_notifications = new List<INotification>();
		}

		public void Add(INotification notification)
		{
			// Add new.
			_notifications.Add(notification);

			// Maintenance.
			Maintenance();
		}

		private void Maintenance()
		{
			lock (_notifications)
			{
				// Remove all expired.
				_notifications.RemoveAll (n => n.HasExpired);

				// Sort.
				_notifications = _notifications.OrderBy (notification => notification.Created).ToList ();
			}
		}
	}
}