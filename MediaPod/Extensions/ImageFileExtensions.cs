using System;
using System.IO;
using System.IO.Abstractions;
using System.Text.RegularExpressions;
using MediaPod.Interfaces.Models;

namespace MediaPod.Model.Extensions
{
	public static class ImageFileExtensions
	{
		public static Base64ImageType ResolveBase64ImageType(this FileInfoBase file)
		{
			switch (file.Extension.ToLower ())
			{
				case ".png":
					return Base64ImageType.PNG;
				case ".jpg":
				case ".jpeg":
					return Base64ImageType.JPEG;
				case ".gif":
					return Base64ImageType.GIF;
				default:
					return Base64ImageType.Unknown;
			}
		}

		public static string ResolveFileExtension(this IBase64Image base64Image)
		{
			switch (base64Image.Type)
			{
				case Base64ImageType.PNG:
					return ".png";
				case Base64ImageType.JPEG:
					return ".jpeg";
				case Base64ImageType.GIF:
					return ".gif";
				default:
					return string.Empty;
			}
		}

		public static IBase64Image ToBase64Image(this FileInfoBase file)
		{
			var imageType = file.ResolveBase64ImageType();
			using (var fileStream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read))
			{
				var buffer = new byte[fileStream.Length];
				fileStream.Read(buffer, 0, (int)fileStream.Length);
				var imageData = Convert.ToBase64String(buffer);
				return new Base64Image(imageType, imageData);
			}
		}

		public static FileInfoBase ToFile(this IBase64Image base64Image, IFileSystem fileSystem)
		{
			var imageTempPath = fileSystem.Path.GetTempFileName();
			var imagePath = imageTempPath + base64Image.ResolveFileExtension();
			fileSystem.File.Move(imageTempPath, imagePath);
			using (var fileStream = new FileStream(imagePath, FileMode.Create, FileAccess.Write))
			{
				var bytes = Convert.FromBase64String(base64Image.Data);
				fileStream.Write(bytes, 0, bytes.Length);
			}
			return fileSystem.FileInfo.FromFileName(imagePath);
		}

		private class Base64Image : IBase64Image
		{
			public Base64ImageType Type { get; private set; }
			public string Data { get; private set; }
			public Base64Image (Base64ImageType type, string data)
			{
				Type = type;
				Data = data;
			}
		}
	}
}