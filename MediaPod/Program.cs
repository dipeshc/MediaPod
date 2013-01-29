using System;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using NDesk.Options;
using ManyConsole;
using MediaPod.Managers;
using MediaPod.Web;

namespace MediaPod
{
	class Program
	{
		public static void Main(string[] args)
		{
			// Run the command for the console input.
			ConsoleCommandDispatcher.DispatchCommand(ConsoleCommandDispatcher.FindCommandsInSameAssemblyAs(typeof(Program)), args, System.Console.Out);
		}
	}

	public class RunCommand : ConsoleCommand
	{
		private static IFileSystem _fileSystem = new FileSystem();

		private int webserverPort;
		private DirectoryInfoBase tvShowDictionary = null;
		private DirectoryInfoBase unorganisedMediaDictionary = null;
		private string tvdbApiKey;

		public RunCommand()
		{
			IsCommand("run", "Run MediaPod.");
			HasRequiredOption("p|port=", "The port on which the MediaPod's webserver will list.", a => webserverPort = int.Parse(a));
			HasRequiredOption("t|tvShowDictionary=", "The directory for the TV Show library.", a => tvShowDictionary = _fileSystem.DirectoryInfo.FromDirectoryName(a));
			HasRequiredOption("u|unorganisedMediaDictionary=", "The directory where unorganised media can be found.", a => unorganisedMediaDictionary = _fileSystem.DirectoryInfo.FromDirectoryName(a));
			HasRequiredOption("k|tvdbApiKey=", "The ApiKey to use when connecting to the TVDB.", a => tvdbApiKey = a);
		}
		
		public override int Run(string[] remainingArguments)
		{
			// Check directories exists.
			if(!tvShowDictionary.Exists)
			{
				throw new ApplicationException("Invalid tvShowDictionary path provided.");
			}
			if(!unorganisedMediaDictionary.Exists)
			{
				throw new ApplicationException("Invalid unorganisedMediaDictionary directory provided.");
			}

			// Initialise.
			ResourceManager.Initialise(_fileSystem, webserverPort, tvShowDictionary, unorganisedMediaDictionary, tvdbApiKey);

			// Retun 0.
			return 0;
		}
	}
}