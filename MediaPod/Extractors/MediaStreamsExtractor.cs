using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Text.RegularExpressions;
using MediaPod.Externals;

namespace MediaPod.Extractors
{
	public class MediaStreamsExtractor
	{
		public enum CodecType
		{
			Unknown,
			H264,
			AAC,
			AC3,
			DTS,
			JPEG,
			PNG
		}

		public interface IStream
		{
			int Id { get; }
			CodecType Codec { get; }
		}

		public interface IVideoStream : IStream
		{
			int Width { get; }
			int Height { get; }
		}

		public interface IAudioStream : IStream
		{
			int Channels { get; }
		}

		public interface ISubtitleStream : IStream
		{
		}

		private class VideoStream : IVideoStream
		{
			public int Id { get; set; }
			public CodecType Codec { get; set; }
			public int Width { get; set; }
			public int Height { get; set; }
		}

		private class AudioStream : IAudioStream
		{
			public int Id { get; set; }
			public CodecType Codec { get; set; }
			public int Channels { get; set; }
		}

		private class SubtitleStream : ISubtitleStream
		{
			public int Id { get; set; }
			public CodecType Codec { get; set; }
		}

		public FileInfoBase File;

		public MediaStreamsExtractor (FileInfoBase file)
		{
			File = file;
		}

		public List<IStream> Extract()
		{
			var output = External.Run(External.Application.FFmpeg, string.Format("-i \"{0}\"", File.FullName));

			int id = 0;
			var streams = new List<IStream>();
			foreach(var line in output.StdErr.Split('\n'))
			{
				// Skip non-stream lines.
				if(!line.Contains("Stream #0"))
				{
					continue;
				}

				if(line.Contains("Video:"))
				{
					streams.Add(MakeVideoStream(id, line));
				}
				else if(line.Contains("Audio:"))
				{
					streams.Add(MakeAudioStream(id, line));
				}
				else if(line.Contains("Subtitle:"))
				{
					streams.Add(new SubtitleStream() { Id = id });
				}
				id++;
			}
			return streams;
		}

		private VideoStream MakeVideoStream(int id, string line)
		{
			var stream = new VideoStream ();
			stream.Id = id;

			if (line.Contains ("h264"))
			{
				stream.Codec = CodecType.H264;
			}
			else if (line.Contains ("jpeg"))
			{
				stream.Codec = CodecType.JPEG;
			}
			else if (line.Contains ("png"))
			{
				stream.Codec = CodecType.PNG;
			}
			
			var dimensionsMatch = new Regex (@"(?<Width>\d{2,})x(?<Height>\d{2,})").Match (line);
			stream.Width = Int32.Parse (dimensionsMatch.Groups ["Width"].Value);
			stream.Height = Int32.Parse (dimensionsMatch.Groups ["Height"].Value);

			return stream;
		}

		private AudioStream MakeAudioStream(int id, string line)
		{
			var stream = new AudioStream ();
			stream.Id = id;

			if(line.Contains ("aac"))
			{
				stream.Codec = CodecType.AAC;
			}
			else if(line.Contains("ac-3"))
			{
				stream.Codec = CodecType.AC3;
			}
			else if(line.Contains("DTS"))
			{
				stream.Codec = CodecType.DTS;
			}

			if(line.Contains("stereo"))
			{
				stream.Channels = 2;
			}
			else if (line.Contains("5.1"))
			{
				stream.Channels = 6;
			}

			return stream;
		}
	}
}