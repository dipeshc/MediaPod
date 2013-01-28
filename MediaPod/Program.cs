using System;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using MediaPod.Managers;
using MediaPod.Web;

namespace MediaPod
{
	class Program
	{
		public static void Main(string[] args)
		{
			// Make filesystem and initialise.
			var fileSystem = new FileSystem();

			// Variables.
			var webserverPort = 8888;
			var unorganisedMediaDictionary = fileSystem.DirectoryInfo.FromDirectoryName("/Users/Dipesh/Media/RAW/");
			var tvShowDictionary = fileSystem.DirectoryInfo.FromDirectoryName("/Users/Dipesh/Media/TV Shows/");
			var tvdbApiKey = "416920BF8A4C278C";

			// Initialise.
			ResourceManager.Initialise(fileSystem, webserverPort, tvShowDictionary, unorganisedMediaDictionary, tvdbApiKey);
		}
	}
}