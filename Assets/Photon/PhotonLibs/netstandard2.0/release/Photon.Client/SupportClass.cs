using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Photon.Client.StructWrapping;

namespace Photon.Client;

/// <summary>
/// Contains several (more or less) useful static methods, mostly used for debugging.
/// </summary>
public class SupportClass
{
	/// <summary>
	/// Class to wrap static access to the random.Next() call in a thread safe manner.
	/// </summary>
	public class ThreadSafeRandom
	{
		private static readonly Random _r = new Random();

		public static int Next()
		{
			lock (_r)
			{
				return _r.Next();
			}
		}
	}

	private static uint[] crcLookupTable;

	public static List<MethodInfo> GetMethods(Type type, Type attribute)
	{
		List<MethodInfo> fittingMethods = new List<MethodInfo>();
		if (type == null)
		{
			return fittingMethods;
		}
		MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		foreach (MethodInfo methodInfo in methods)
		{
			if (attribute == null || methodInfo.IsDefined(attribute, inherit: false))
			{
				fittingMethods.Add(methodInfo);
			}
		}
		return fittingMethods;
	}

	/// <summary>
	/// Writes the exception's stack trace to the received stream.
	/// </summary>
	/// <param name="throwable">Exception to obtain information from.</param>
	/// <param name="stream">Output sream used to write to.</param>
	public static void WriteStackTrace(Exception throwable, TextWriter stream = null)
	{
		if (stream != null)
		{
			stream.WriteLine(throwable.ToString());
			stream.WriteLine(throwable.StackTrace);
			stream.Flush();
		}
	}

	/// <summary>
	/// This method returns a string, representing the content of the given IDictionary.
	/// Returns "null" if parameter is null.
	/// </summary>
	/// <param name="dictionary">IDictionary to return as string.</param>
	/// <param name="includeTypes"> </param>
	public static string DictionaryToString(IDictionary dictionary, bool includeTypes = true)
	{
		if (dictionary == null)
		{
			return "null";
		}
		StringBuilder sb = new StringBuilder();
		sb.Append("{");
		foreach (object key in dictionary.Keys)
		{
			if (sb.Length > 1)
			{
				sb.Append(", ");
			}
			Type valueType;
			string value;
			if (dictionary[key] == null)
			{
				valueType = typeof(object);
				value = "null";
			}
			else
			{
				valueType = dictionary[key].GetType();
				value = dictionary[key].ToString();
			}
			if (valueType == typeof(IDictionary) || valueType == typeof(PhotonHashtable))
			{
				value = DictionaryToString((IDictionary)dictionary[key]);
			}
			else if (valueType == typeof(NonAllocDictionary<byte, object>))
			{
				value = DictionaryToString((NonAllocDictionary<byte, object>)dictionary[key]);
			}
			else if (valueType == typeof(string[]))
			{
				value = string.Format("{{{0}}}", string.Join(",", (string[])dictionary[key]));
			}
			else if (valueType == typeof(byte[]))
			{
				value = $"byte[{((byte[])dictionary[key]).Length}]";
			}
			else if (dictionary[key] is StructWrapper sw)
			{
				sb.AppendFormat("{0}={1}", key, sw.ToString(includeTypes));
				continue;
			}
			if (includeTypes)
			{
				sb.AppendFormat("({0}){1}=({2}){3}", key.GetType().Name, key, valueType.Name, value);
			}
			else
			{
				sb.AppendFormat("{0}={1}", key, value);
			}
		}
		sb.Append("}");
		return sb.ToString();
	}

	public static string DictionaryToString(NonAllocDictionary<byte, object> dictionary, bool includeTypes = true)
	{
		if (dictionary == null)
		{
			return "null";
		}
		StringBuilder sb = new StringBuilder();
		sb.Append("{");
		foreach (byte key in dictionary.Keys)
		{
			if (sb.Length > 1)
			{
				sb.Append(", ");
			}
			Type valueType;
			string value;
			if (dictionary[key] == null)
			{
				valueType = typeof(object);
				value = "null";
			}
			else
			{
				valueType = dictionary[key].GetType();
				value = dictionary[key].ToString();
			}
			if (valueType == typeof(IDictionary) || valueType == typeof(PhotonHashtable))
			{
				value = DictionaryToString((IDictionary)dictionary[key]);
			}
			else if (valueType == typeof(NonAllocDictionary<byte, object>))
			{
				value = DictionaryToString((NonAllocDictionary<byte, object>)dictionary[key]);
			}
			else if (valueType == typeof(string[]))
			{
				value = string.Format("{{{0}}}", string.Join(",", (string[])dictionary[key]));
			}
			else if (valueType == typeof(byte[]))
			{
				value = $"byte[{((byte[])dictionary[key]).Length}]";
			}
			else if (dictionary[key] is StructWrapper sw)
			{
				sb.AppendFormat("{0}={1}", key, sw.ToString(includeTypes));
				continue;
			}
			if (includeTypes)
			{
				sb.AppendFormat("({0}){1}=({2}){3}", key.GetType().Name, key, valueType.Name, value);
			}
			else
			{
				sb.AppendFormat("{0}={1}", key, value);
			}
		}
		sb.Append("}");
		return sb.ToString();
	}

	/// <summary>
	/// Converts a byte-array to string (useful as debugging output).
	/// Uses BitConverter.ToString(list) internally after a null-check of list.
	/// </summary>
	/// <param name="list">Byte-array to convert to string.</param>
	/// <param name="length">Length of bytes to convert to string. If negative, list.Length is converted. Optional. Default: -1. </param>
	/// <returns>
	/// List of bytes as string.
	/// </returns>
	public static string ByteArrayToString(byte[] list, int length = -1)
	{
		if (list == null)
		{
			return string.Empty;
		}
		if (length < 0 || length > list.Length)
		{
			length = list.Length;
		}
		return BitConverter.ToString(list, 0, length);
	}

	private static uint[] InitializeTable(uint polynomial)
	{
		uint[] createTable = new uint[256];
		for (int i = 0; i < 256; i++)
		{
			uint entry = (uint)i;
			for (int j = 0; j < 8; j++)
			{
				entry = (((entry & 1) != 1) ? (entry >> 1) : ((entry >> 1) ^ polynomial));
			}
			createTable[i] = entry;
		}
		return createTable;
	}

	public static uint CalculateCrc(byte[] buffer, int offset, int length)
	{
		uint num = uint.MaxValue;
		uint polynomial = 3988292384u;
		if (crcLookupTable == null)
		{
			crcLookupTable = InitializeTable(polynomial);
		}
		for (int i = 0; i < length; i++)
		{
			num = (num >> 8) ^ crcLookupTable[buffer[offset + i] ^ (num & 0xFF)];
		}
		return num;
	}

	[Obsolete("Use overloaded CalculateCrc version with offset.")]
	public static uint CalculateCrc(byte[] buffer, int length)
	{
		return CalculateCrc(buffer, 0, length);
	}
}
