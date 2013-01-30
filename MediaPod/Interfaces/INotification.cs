using System;

namespace MediaPod.Interfaces
{
	public enum NotificationType
	{
		Error,
		Warning,
		Success,
		Informational
	}

	public interface INotification
	{
		string Id { get; }
		string Heading { get; }
		string Message { get; }
		NotificationType Type { get; }
		DateTime Created { get; }
		bool IsSeen { get; }
		bool HasExpired { get; }

		void Seen();
	}
}