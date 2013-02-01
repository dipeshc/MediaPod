using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text.RegularExpressions;
using MediaPod.Interfaces.Models;
using MediaPod.Managers;
using MediaPod.Model.Extensions;
using ServiceStack.Common.Web;
using ServiceStack.Text;

namespace MediaPod.Api.Extensions
{
	public static class ApiExtensions
	{
		// TODO. Swap out for global filesystem.
		private static IFileSystem _fileSystem = new FileSystem();
		public static IFileSystem FileSystem { get { return _fileSystem; } }

		public static void TransmitFile(this Stream outputStream, FileInfoBase file, long offset, long length)
		{
			var currentPosition = offset;
			var end = offset + length;
			byte[] buffer = new byte[4096];
			var bufferContentSize = 0;
			using (var fileStream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read))
			{
				fileStream.Seek(offset, SeekOrigin.Begin);
				while ((bufferContentSize = fileStream.Read (buffer, 0, buffer.Length)) > 0)
				{
					currentPosition += bufferContentSize;
					var bufferTransmitCount = currentPosition > end ? Convert.ToInt32(end - currentPosition) : bufferContentSize;
					outputStream.Write (buffer, 0, bufferTransmitCount);
				}
			}
		}

		public static string ToSiteFilePath(this FileInfoBase file)
		{
			var filePath = file.FullName;
			var tvShowLibraryPath = ResourceManager.TVShowLibrary.RootDirectory.FullName;
			var unorganisedLibraryPath = ResourceManager.UnorganisedLibrary.RootDirectory.FullName;
			
			if (filePath.StartsWith (tvShowLibraryPath))
			{
				var unrootedPath =  new Regex(string.Format(@"{0}{1}?", unorganisedLibraryPath, FileSystem.Path.DirectorySeparatorChar)).Replace(filePath, "");
				return FileSystem.Path.Combine("TVShowLibrary/", unrootedPath);
			}
			if (filePath.StartsWith(unorganisedLibraryPath))
			{
				var unrootedPath =  new Regex(string.Format(@"{0}{1}?", unorganisedLibraryPath, FileSystem.Path.DirectorySeparatorChar)).Replace(filePath, "");
				return FileSystem.Path.Combine("UnorganisedLibrary/", unrootedPath);
			}
			
			return filePath;
		}
		
		public static FileInfoBase FromSiteFilePath(this string siteFilePath)
		{
			var siteFilePathRaw = siteFilePath.UrlDecode();
			
			if(siteFilePathRaw.StartsWith("TVShowLibrary/"))
			{
				var manager = ResourceManager.TVShowLibrary;
				var expectedFilePath = manager.RootDirectory.FullName + siteFilePathRaw.Replace("TVShowLibrary/", "");
				return manager.TVShowCollections.SelectMany(tvShowCollection => tvShowCollection).Select(tvShow => tvShow.File).FirstOrDefault(file => file.FullName == expectedFilePath);
			}
			if(siteFilePathRaw.StartsWith("UnorganisedLibrary/"))
			{
				var expectedFilePath = ResourceManager.UnorganisedLibrary.RootDirectory.FullName + siteFilePathRaw.Replace("UnorganisedLibrary/", "");
				return ResourceManager.UnorganisedLibrary.Get().Select(media => media.File).FirstOrDefault(file => file.FullName == expectedFilePath);
			}
			return FileSystem.FileInfo.FromFileName(siteFilePath);
		}

		public static Base64ImageType ResolveBase64ImageType(this string contentType)
		{
			switch (contentType.ToLower ())
			{
				case "image/png":
					return Base64ImageType.PNG;
				case "image/jpeg":
					return Base64ImageType.JPEG;
				case "image/gif":
					return Base64ImageType.GIF;
				default:
					return Base64ImageType.Unknown;
			}
		}

		public static string ToWebImage(this IBase64Image base64Image)
		{
			var contentType = MimeTypes.GetMimeType(base64Image.ResolveFileExtension());
			return string.Format("data:{0};base64,{1}", contentType, base64Image.Data);
		}

		public static IBase64Image FromWebImage(this string base64WebImageString)
		{
			var match = new Regex ("data:(?<Type>[^;]+);base64,(?<Data>.+)").Match (base64WebImageString);
			if (!match.Success)
			{
				return null;
			}
			return new Base64Image()
			{
				Type = match.Groups["Type"].Value.ResolveBase64ImageType(),
				Data = match.Groups["Data"].Value
			};
		}

		private class Base64Image : IBase64Image
		{
			public Base64ImageType Type { get; set; }
			public string Data { get; set; }
		}
	}
}

