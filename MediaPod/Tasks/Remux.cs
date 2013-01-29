using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Reflection;
using System.Text;
using MediaPod.Externals;
using MediaPod.Extractors;
using MediaPod.Interfaces.Models;

namespace MediaPod.Tasks
{
	public class Remux : BaseTask
	{
		private static int idCounter = 0;

		public Remux (IFileSystem fileSystem, FileInfoBase inputFile, FileInfoBase outputFile, TextWriter stdOut=null, TextWriter stdErr=null) : base("Remux" + idCounter++, stdOut, stdErr)
		{
			_action = () =>
			{
				// Create and set working area.
				var workingArea = fileSystem.DirectoryInfo.FromDirectoryName (fileSystem.Path.Combine (fileSystem.Path.Combine (fileSystem.Path.GetTempPath (), Assembly.GetExecutingAssembly().GetName().Name), this.GetType().Name));
				if (!workingArea.Exists)
				{
					LogOutput("Creating working area at {0}.", workingArea.FullName);
					workingArea.Create();
				}

				// Get streams and split.
				var streams = new MediaStreamsExtractor(inputFile).Extract().OrderBy(steam => steam.Id);
				var videoStreams = streams.Where(stream => stream is MediaStreamsExtractor.IVideoStream).Select(stream => (MediaStreamsExtractor.IVideoStream) stream).ToList();
				var audioStreams = streams.Where(stream => stream is MediaStreamsExtractor.IAudioStream).Select(stream => (MediaStreamsExtractor.IAudioStream) stream).ToList();
				var subtitleStreams = streams.Where(stream => stream is MediaStreamsExtractor.ISubtitleStream).Select(stream => (MediaStreamsExtractor.ISubtitleStream) stream).ToList();

				// Make working area file.
				var name = outputFile.Name.Replace(outputFile.Extension, "");
				var name1 = name + ".stage1";
				if(string.Compare(outputFile.Extension, ".m4v", System.StringComparison.OrdinalIgnoreCase)==0)
				{
					name1 += ".mp4";
				}
				else
				{
					name1 += outputFile.Extension;
				}
				var name2 = name + ".stage2" + outputFile.Extension;
				var workingAreaStage1File = fileSystem.FileInfo.FromFileName(fileSystem.Path.Combine(workingArea.FullName, name1));
				var workingAreaStage2File = fileSystem.FileInfo.FromFileName(fileSystem.Path.Combine(workingArea.FullName, name2));


				// Working variables.
				var maps = new StringBuilder();
				var codecs = new StringBuilder();
				var outputStreamIndex = 0;

				// Copy video streams.
				var h264 = videoStreams.Where(stream => stream.Codec == MediaStreamsExtractor.CodecType.H264).ToList();
				if(h264.Any())
				{
					h264.Where(stream => stream.Codec == MediaStreamsExtractor.CodecType.H264).ToList().ForEach(stream => MakeCopyStream(stream, maps, codecs, outputStreamIndex++));
				}
				else
				{
					LogError("Unable to find h264 stream. Will not remux.");
					return;
				}

				// Copy or transcode aac 2 channel audio streams.
				var acc2Channel = audioStreams.Where(stream => stream.Codec == MediaStreamsExtractor.CodecType.AAC && stream.Channels == 2).ToList();
				if(acc2Channel.Any())
				{
					acc2Channel.ForEach(stream => MakeCopyStream(stream, maps, codecs, outputStreamIndex++));
				}
				else
				{
					// Transcode aac 2 channel audio stream from all available audio streams.
					audioStreams.ForEach(stream =>
					{
						maps.AppendFormat(" -map 0:{0} ", stream.Id);
						codecs.AppendFormat(" -c:{0} aac -ac:a 2 ", outputStreamIndex++);
					});
				}

				// Copy or transcode higher quality audio stream if possible.
				var ac36Channel = audioStreams.Where(stream => stream.Codec == MediaStreamsExtractor.CodecType.AC3 && stream.Channels == 6).ToList();
				var dts = audioStreams.Where(stream => stream.Codec == MediaStreamsExtractor.CodecType.DTS).ToList();

				// Copy ac3 6 channel streams if exists.
				if(ac36Channel.Any())
				{
					ac36Channel.ForEach(stream => MakeCopyStream(stream, maps, codecs, outputStreamIndex++));
				}
				else if(dts.Any())
				{
					// Transcode ac3 6 channel audio streams from dts streams.
					dts.ForEach(stream =>
                	{
						maps.AppendFormat(" -map 0:{0} ", stream.Id);
						codecs.AppendFormat(" -c:{0} ac3 -ac:a 6 ", outputStreamIndex++);
					});
				}

				// Copy dts streams if exists.
				dts.ForEach(stream => MakeCopyStream(stream, maps, codecs, outputStreamIndex++));

				// Transcode in subtitles if exists.
				subtitleStreams.ForEach(stream =>
				{
					maps.AppendFormat(" -map 0:{0} ", stream.Id);
					codecs.AppendFormat(" -c:{0} mov_text ", outputStreamIndex++);
				});

				// Mov atom detils.
				var movFlags = "";
				if(string.Compare(outputFile.Extension, ".m4v", System.StringComparison.OrdinalIgnoreCase)==0
				   || string.Compare(outputFile.Extension, ".mp4", System.StringComparison.OrdinalIgnoreCase)==0)
				{
					movFlags = " -movflags faststart ";
				}

				// Remux.
				var remuxArguments = string.Format("-i \"{0}\" -y -strict experimental {1} {2} {3} \"{4}\"", inputFile.FullName, maps, codecs, movFlags, workingAreaStage1File.FullName);
				LogOutput("Running FFmpeg with arguments {0}", remuxArguments);
				External.Run(External.Application.FFmpeg, remuxArguments, StdOut, StdErr);

				// Optimise tracks.
				var sublerArguments = string.Format ("-source \"{0}\" -dest \"{1}\" -optimize -itunesfriendly", workingAreaStage1File.FullName, workingAreaStage2File.FullName);
				LogOutput("Running Subler to optimize track with arguments {0}", sublerArguments);
				External.Run(External.Application.SublerCLI, sublerArguments, StdOut, StdErr);

				// Delete stage1 file.
				LogOutput("Deleting workingAreaFile1 {0}", workingAreaStage1File.FullName);
				workingAreaStage1File.Delete();

				// Delete output file if it already exists.
				if(outputFile.Exists)
				{
					LogOutput("Output file {0} already exists. Will delete.", outputFile.FullName);
					outputFile.Delete();
				}

				// Move stage 2 file to output file location.
				LogOutput("Moving newly created remux file {0} to output file location {1}.", workingAreaStage2File.FullName, outputFile.FullName);
				workingAreaStage2File.MoveTo(outputFile.FullName);
			};
		}

		private static void MakeCopyStream(MediaStreamsExtractor.IStream stream, StringBuilder maps, StringBuilder codecs, int outputStreamIndex)
		{
			maps.AppendFormat(" -map 0:{0} ", stream.Id);
			codecs.AppendFormat(" -c:{0} copy ", outputStreamIndex);
		}
	}
}

