using System;
using System.IO;
using System.IO.Abstractions;
using MediaPod.Interfaces;
using MediaPod.Managers;

namespace MediaPod.Tasks
{
	public abstract class BaseTask : ITask
	{
		public string Id { get; private set; }
		protected Action _action { get; set; }
		public TextWriter StdOut { get; set; }
		public TextWriter StdErr { get; set; }

		public bool HasStarted { get; protected set; }
		public DateTime Created { get; protected set; }
		public DateTime Started { get; protected set; }
		public DateTime Ended { get; protected set; }

		private TextWriter _managedLog;

		protected BaseTask (string id, TextWriter stdOut=null, TextWriter stdErr=null)
		{
			Id = id;
			Created = DateTime.UtcNow;
			StdOut = stdOut;
			StdErr = stdErr;
			if (StdOut == null || StdErr == null)
			{
				_managedLog = ResourceManager.LogManager.GetStdOutLog();
				StdOut = _managedLog;
				StdErr = _managedLog;
			}
			LogOutput("Created.");
		}

		public void Invoke()
		{
			LogOutput ("Started");
			Started = DateTime.UtcNow;
			HasStarted = true;
			_action.Invoke ();
			Ended = DateTime.UtcNow;
			LogOutput ("Completed");

			if (_managedLog != null)
			{
				_managedLog.Close();
			}
		}

		protected void LogOutput(string format, params object[] args)
		{
			StdOut.WriteLine(string.Format("{0} - {1} - {2}", DateTime.UtcNow.ToString("o"), Id, string.Format(format, args)));
			StdOut.Flush();
		}

		protected void LogError(string format, params object[] args)
		{
			StdErr.WriteLine(string.Format("{0} - {1} - {2}", DateTime.UtcNow.ToString("o"), Id, string.Format(format, args)));
			StdErr.Flush();
		}
	}
}

