using System;
using System.Collections.Generic;
using MediaPod.Interfaces;

namespace MediaPod.Managers
{
	public class NotificationManager
	{
		private readonly List<INotification> _notifications;
		public IEnumerable<INotification> Notifications
		{
			get
			{
				Clean();
				return _notifications;
			}
		}

		public NotificationManager()
		{
			_notifications = new List<INotification>();
		}

		public void NewNotification(INotification notification)
		{
			// Do clean.
			Clean();

			// Add new.
			_notifications.Add(notification);
		}

		private void Clean()
		{
			// Remove all expired.
			_notifications.RemoveAll(n => n.HasExpired);
		}
	}
}