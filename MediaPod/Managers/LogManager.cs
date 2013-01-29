using System;
using System.IO;
using System.IO.Abstractions;
using System.Text;

namespace MediaPod.Managers
{
	public class LogManager
	{
		private enum Mode
		{
			File,
			Stream
		}
		
		private enum Type
		{
			StdOut,
			StdErr
		}
		
		private Random _random = new Random((int)DateTime.Now.Ticks);
		private Mode _mode;
		
		private IFileSystem _fileSystem;
		private DirectoryInfoBase _logDirectory;
		
		private TextWriter _stdOut;
		private TextWriter _stdErr;
		
		public LogManager (TextWriter stdOut, TextWriter stdErr)
		{
			_mode = Mode.Stream;
			_stdOut = stdOut;
			_stdErr = stdErr;
		}
		
		public LogManager (IFileSystem fileSystem, DirectoryInfoBase logDirectory)
		{
			_mode = Mode.File;
			_fileSystem = fileSystem;
			_logDirectory = logDirectory;
		}
		
		public TextWriter GetStdOutLog()
		{
			return GetLog(Type.StdOut);
		}
		
		public TextWriter GetStdErrLog()
		{
			return GetLog(Type.StdErr);
		}
		
		private TextWriter GetLog(Type type)
		{
			if (_mode == Mode.Stream)
			{
				return type==Type.StdOut?_stdOut:_stdErr;
			}
			
			var name = string.Format("{0}{1}{2}.log", type==Type.StdOut?"StdOut":"StdErr", DateTime.UtcNow.ToString("o"), RandomString(8));
			var logFilePath = _fileSystem.Path.Combine(_logDirectory.FullName, name);
			return new StreamWriter(logFilePath, true);
		}
		
		private string RandomString(int size)
		{
			StringBuilder builder = new StringBuilder();
			char ch;
			for (int i = 0; i < size; i++)
			{
				ch = Convert.ToChar(Convert.ToInt32(Math.Floor(26 * _random.NextDouble() + 65)));                 
				builder.Append(ch);
			}
			return builder.ToString();
		}
	}
}