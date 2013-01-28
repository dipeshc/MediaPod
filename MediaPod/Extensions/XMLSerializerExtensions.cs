using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace System.Xml.Serialization.Extensions
{
	public static class XMLSerializeExtensions
	{	
		public static void XMLSerializeAsSet<T>(this IEnumerable<T> data, FileInfoBase file)
		{
			// Serialize.
			using (var Writer = new StreamWriter(file.FullName))
			{
				new XmlSerializer(typeof(List<T>)).Serialize(Writer, data);
			}
		}
		
		public static IEnumerable<T> XMLDeserializeFile<T>(this FileInfoBase file)
		{
			// Deserialize.
			using (var Reader = new StreamReader(file.FullName))
			{
				return (IEnumerable<T>) new XmlSerializer(typeof(List<T>)).Deserialize(Reader);
			}
		}
	}
}