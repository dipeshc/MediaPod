using System.IO.Abstractions;

namespace MediaPod.Interfaces.Models
{
	public enum Definition
	{
		Unknown = -1,
		SD = 0,
		HD720 = 1,
		HD1080 = 2
	}

	public interface IMediaFile
	{
		FileInfoBase File { get; }
		Definition Definition { get; }
	}
}