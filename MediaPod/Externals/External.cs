using System;
using System.Diagnostics;
using System.IO;
using System.IO.Abstractions;
using System.Reflection;
using Mono.Unix.Native;

namespace MediaPod.Externals
{
	public static class External 
	{
		public enum Application
		{
			SublerCLI,
			FFmpeg
		}

		public class Output
		{
			public int ExitCode;
			public string StdOut;
			public string StdErr;
		}

		public static Output Run(Application application, string arguments, TextWriter stdOut = null, TextWriter stdErr = null)
		{
			// Set output.
			stdOut = stdOut ?? Console.Out;
			stdErr = stdErr ?? Console.Error;

			// Create StartInfo.
			var processStartInfo = new ProcessStartInfo ();
			processStartInfo.FileName = GetExecutable(application).FullName;
			processStartInfo.Arguments = arguments;
			processStartInfo.UseShellExecute = false;
			processStartInfo.RedirectStandardOutput = true;
			processStartInfo.RedirectStandardError = true;

			// Run.
			using (var process = Process.Start(processStartInfo))
			{
				// Make output.
				var output = new Output();

				// Redirect and save output.
				process.OutputDataReceived += (sender, e) =>
				{
					output.StdOut += e.Data + '\n';
					stdOut.WriteLine(e.Data);
					stdOut.Flush();
				};
				process.ErrorDataReceived += (sender, e) =>
				{
					output.StdErr += e.Data + '\n';
					stdErr.WriteLine(e.Data);
					stdErr.Flush();
				};

				// Wait for process to exit.
				process.BeginOutputReadLine();
				process.BeginErrorReadLine();
				process.WaitForExit();

				// Set exit code.
				output.ExitCode = process.ExitCode;

				// Return.
				return output;
			}
		}

		private static FileInfoBase GetExecutable(Application application)
		{
			//TODO: Switch out for global filesystem.
			var _fileSystem = new FileSystem();
			
			// Create executable file path.
			var assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
			var file = _fileSystem.FileInfo.FromFileName(_fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), assemblyName + _fileSystem.Path.DirectorySeparatorChar + application));
			
			// Create directory if required.
			if(!file.Directory.Exists)
			{
				file.Directory.Create();
			}
			
			// Extract file if does not exist.
			if(!file.Exists)
			{
				// Read bytes from assembly and create the file.
				var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("MediaPod.Externals." + application);
				var bytes = new byte[(int)stream.Length];
				stream.Read(bytes, 0, bytes.Length);
				_fileSystem.File.WriteAllBytes(file.FullName, bytes);
			}
			
			// Set permissions.
			Syscall.chmod(file.FullName, FilePermissions.S_IRWXU);
			
			// Set _executable.
			return file;
		}
	}
}

