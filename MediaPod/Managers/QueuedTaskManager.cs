using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using MediaPod.Interfaces;

namespace MediaPod.Managers
{
	public class QueuedTaskManager
	{
		private Queue<ITask> _tasks = new Queue<ITask>();
		public ITask CurrentTask { get; private set; }

		public void Enqueue(ITask task)
		{
			_tasks.Enqueue(task);
		}

		public void Start()
		{
			new Thread(() =>
			{
				while(true)
				{
					if(_tasks.Count!=0)
					{
						CurrentTask = _tasks.Dequeue();
						CurrentTask.Invoke();
					}
					else
					{
						Thread.Sleep(1000); // 1sec.
					}
				}
			}).Start();
		}

		public IEnumerable<ITask> GetQueuedTasks()
		{
			return _tasks;
		}
	}
}