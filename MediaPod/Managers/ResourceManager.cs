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
		public static QueuedTaskManager QueuedTaskManager;
		public static UnorganisedLibrary UnorganisedLibrary;
		public static TVShowLibrary TVShowLibrary;
		public static ITVShowMetadataSource MetadataSource;
		public static UnorganisedLibrary UnorganisedLibraryLibrary;
		public static WebserverManager WebserverManager;

		private static Thread _keepAliveThread;
		private static Thread _fileSystemReloaderThread;
		private static Thread _webServerThread;
		private const int _keepAliveSleepTime = 1000 * 5; // 5sec.
		private const int _fileSystemReloaderSleepTime = 1000 * 10; // 10sec.

		public static void Initialise(IFileSystem fileSystem, int webserverPort, DirectoryInfoBase tvShowDictionary, DirectoryInfoBase unorganisedDictionary, string tvdbApiKey)
		{
			// Make sources.
			var metadataSource = new TVDBTVShowMetadataSource(fileSystem, tvdbApiKey);
			MetadataSource = metadataSource;
			var metadataSources = new List<ITVShowMetadataSource>() { metadataSource };

			// Initalise.
			QueuedTaskManager = new QueuedTaskManager();
			UnorganisedLibrary = new UnorganisedLibrary(unorganisedDictionary);
			TVShowLibrary = new TVShowLibrary(tvShowDictionary, metadataSources);
			WebserverManager = new WebserverManager(webserverPort);

			QueuedTaskManager.Start();

			// Setup reloads.
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
				if(_webServerThread==null || !_webServerThread.IsAlive)
				{
					_webServerThread = new Thread(() =>
					{
						// Run webserver and block from terminating.
						WebserverManager.Run();
						System.Threading.Thread.Sleep(System.Threading.Timeout.Infinite);
					});
					_webServerThread.Priority = ThreadPriority.Normal;
					_webServerThread.Start();
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

