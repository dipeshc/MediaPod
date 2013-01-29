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
			ConsoleCommandDispatcher.DispatchCommand(ConsoleCommandDispatcher.FindCommandsInSameAssemblyAs(typeof(Program)), args, Console.Out);
		}
	}

	public class RunCommand : ConsoleCommand
	{
		private IFileSystem _fileSystem = new FileSystem();
		private int _webserverPort;
		private DirectoryInfoBase _tvShowDirectory = null;
		private DirectoryInfoBase _unorganisedMediaDirectory = null;
		private string _tvdbApiKey;
		private DirectoryInfoBase _logDirectory = null;

		public RunCommand()
		{
			IsCommand("run", "Run MediaPod.");
			HasRequiredOption("p|port=", "The port on which the MediaPod's webserver will list.", a => _webserverPort = int.Parse(a));
			HasRequiredOption("t|tvShowDictionary=", "The directory for the TV Show library.", a => _tvShowDirectory = _fileSystem.DirectoryInfo.FromDirectoryName(a));
			HasRequiredOption("u|unorganisedMediaDictionary=", "The directory where unorganised media can be found.", a => _unorganisedMediaDirectory = _fileSystem.DirectoryInfo.FromDirectoryName(a));
			HasRequiredOption("k|tvdbApiKey=", "The ApiKey to use when connecting to the TVDB.", a => _tvdbApiKey = a);
			HasOption("l|logDirectory=", "The directory to store the logs.", a => _logDirectory = _fileSystem.DirectoryInfo.FromDirectoryName(a));
		}
		
		public override int Run(string[] remainingArguments)
		{
			// Check directories exists.
			if (!_tvShowDirectory.Exists)
			{
				throw new ApplicationException ("Invalid tvShowDictionary provided. Does not exist.");
			}
			if (!_unorganisedMediaDirectory.Exists)
			{
				throw new ApplicationException ("Invalid unorganisedMediaDictionary provided. Does not exist.");
			}
			if (_logDirectory != null && !_logDirectory.Exists)
			{
				throw new ApplicationException ("Invalid logDirectory provided. Does not exist.");
			}

			// Initialise.
			ResourceManager.Initialise(_fileSystem, _webserverPort, _tvShowDirectory, _unorganisedMediaDirectory, _tvdbApiKey, _logDirectory);

			// Retun 0.
			return 0;
		}
	}
}