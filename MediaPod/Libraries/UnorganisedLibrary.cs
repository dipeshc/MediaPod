using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using MediaPod.Extractors;
using MediaPod.Interfaces.Models;
using MediaPod.Sources;

namespace MediaPod.Libraries
{
	public class UnorganisedLibrary
	{
		public readonly DirectoryInfoBase RootDirectory;

		public UnorganisedLibrary (DirectoryInfoBase rootDirectory)
		{
			RootDirectory = rootDirectory;
		}

		public IEnumerable<IMediaFile> Get()
		{
			// Get all files with matching extension.
			return RootDirectory.GetFiles("*", SearchOption.AllDirectories)
				.Where(file => file.Extension.ToLower().EndsWith(".mp4") || file.Extension.ToLower().EndsWith(".m4v") || file.Extension.ToLower().EndsWith(".mkv"))
				.Select(file => new MediaFile(file));
		}

		private class MediaFile : IMediaFile
		{
			public FileInfoBase File { get; private set; }
			public Definition Definition { get; private set; }
			
			public MediaFile(FileInfoBase file)
			{
				File = file;
				Definition = ExtractDefinition(file);
			}
			
			private static Definition ExtractDefinition(FileInfoBase file)
			{
				var streams = new MediaStreamsExtractor(file).Extract();
				var videoStream = streams.Where(stream => stream is MediaStreamsExtractor.IVideoStream).Select(stream => ((MediaStreamsExtractor.IVideoStream) stream).Height).OrderByDescending(h => h);
				if(!videoStream .Any())
				{
					return Definition.Unknown;
				}
				
				var height = videoStream.First();
				if(height >= 1080)
				{
					return Definition.HD1080;
				}
				else if(height >= 720)
				{
					return Definition.HD720;
				}
				return Definition.SD;
			}
		}
	}
}