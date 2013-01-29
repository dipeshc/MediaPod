using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Abstractions;
using System.Threading;
using MediaPod.Interfaces;
using MediaPod.Sources;
using MediaPod.Libraries;
using MediaPod.Web;

namespace MediaPod.Managers
{
	public static class ResourceManager
	{
		public static LogManager LogManager;
		public static ITVShowMetadataSource MetadataSource;
		public static QueuedTaskManager QueuedTaskManager;
		public static TVShowLibrary TVShowLibrary;
		public static UnorganisedLibrary UnorganisedLibrary;
		public static WebserverManager WebserverManager;

		private static Thread _keepAliveThread;
		private static Thread _fileSystemReloaderThread;
		private static Thread _webserverThread;
		private const int _keepAliveSleepTime = 1000 * 5; // 5sec.
		private const int _fileSystemReloaderSleepTime = 1000 * 10; // 10sec.

		public static void Initialise(IFileSystem fileSystem, int webserverPort, DirectoryInfoBase tvShowDirectory, DirectoryInfoBase unorganisedDirectory, string tvdbApiKey, DirectoryInfoBase logDirectory=null)
		{
			// Initalise.
			if (logDirectory == null)
			{
				LogManager = new LogManager(Console.Out, Console.Error);
			}
			else
			{
				LogManager = new LogManager (fileSystem, logDirectory);
			}
			MetadataSource = new TVDBTVShowMetadataSource(fileSystem, tvdbApiKey);
			QueuedTaskManager = new QueuedTaskManager();
			TVShowLibrary = new TVShowLibrary(tvShowDirectory, new List<ITVShowMetadataSource>() { MetadataSource });
			UnorganisedLibrary = new UnorganisedLibrary(unorganisedDirectory);
			WebserverManager = new WebserverManager(webserverPort);

			// Sta
			QueuedTaskManager.Start();

			// Setup keep alive thread..
			_keepAliveThread = CreateIntervalThread(() =>
			{
				// Create threads if not alive.
				if(_fileSystemReloaderThread==null || !_fileSystemReloaderThread.IsAlive)
				{
					_fileSystemReloaderThread = CreateIntervalThread(() =>
					{
						TVShowLibrary.Load();
					}, _fileSystemReloaderSleepTime);
					_fileSystemReloaderThread.Priority = ThreadPriority.Lowest;
					_fileSystemReloaderThread.Start();
				}
				if(_webserverThread==null || !_webserverThread.IsAlive)
				{
					_webserverThread = new Thread(() =>
					{
						// Run webserver and block from terminating.
						WebserverManager.Run();
						System.Threading.Thread.Sleep(System.Threading.Timeout.Infinite);
					});
					_webserverThread.Priority = ThreadPriority.Normal;
					_webserverThread.Start();
				}
			}, _keepAliveSleepTime);
			
			// Set priority and start.
			_keepAliveThread.Priority = ThreadPriority.Lowest;
			_keepAliveThread.Start();
		}

		private static Thread CreateIntervalThread(Action threadTask, int sleepTime)
		{
			return new Thread(() =>
			{
				try
				{
					while(true)
					{
						threadTask();
						Thread.Sleep(sleepTime);
					}
				}
				catch (Exception exception)
				{
					Console.WriteLine(exception);
				}
			});
		}
	}
}

