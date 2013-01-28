using System;
using System.IO;

namespace MediaPod.Interfaces
{
	public interface ITask
	{
		string Id { get; }
		TextWriter StdOut { get; }
		TextWriter StdErr { get; }

		bool HasStarted { get; }
		DateTime Created { get; }
		DateTime Started { get; }
		DateTime Ended { get; }

		void Invoke();
	}
}