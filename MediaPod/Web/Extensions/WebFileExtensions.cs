/**
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Text.RegularExpressions;
using MediaPod.Interfaces.Models;
using MediaPod.Model.Extensions;

namespace MediaPod.Web.Extensions
{
	public static class WebFileExtensions
	{
		public static string ResoveContentType(this string filePath)
		{
			if (filePath.EndsWith (".js", StringComparison.OrdinalIgnoreCase))
			{
				return "application/ecmascript";
			}
			if (filePath.EndsWith (".css", StringComparison.OrdinalIgnoreCase))
			{
				return "text/css";
			}
			if (filePath.EndsWith (".png", StringComparison.OrdinalIgnoreCase))
			{
				return "image/png";
			}
			if (filePath.EndsWith (".jpg", StringComparison.OrdinalIgnoreCase) || filePath.EndsWith (".jpeg", StringComparison.OrdinalIgnoreCase))
			{
				return "image/jpeg";
			}
			if (filePath.EndsWith (".gif", StringComparison.OrdinalIgnoreCase))
			{
				return "image/gif";
			}
			if (filePath.EndsWith (".mp4", StringComparison.OrdinalIgnoreCase) || filePath.EndsWith (".m4v", StringComparison.OrdinalIgnoreCase))
			{
				return "video/mp4";
			}
			return "";
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

		public static string ToWebImage(this IBase64Image base64Image)
		{
			var contentType = base64Image.ResolveFileExtension().ResoveContentType();
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
**/