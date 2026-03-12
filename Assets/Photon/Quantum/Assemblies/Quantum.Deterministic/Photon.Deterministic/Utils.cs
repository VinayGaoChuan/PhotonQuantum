using System;
using System.Text;

namespace Photon.Deterministic
{
	internal static class Utils
	{
		public static string ToStringArray(this Array array, int offset = 0, int count = int.MaxValue)
		{
			if (array == null)
			{
				return "NULL";
			}
			if (array.Length == 0)
			{
				return "[]";
			}
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("[");
			for (int i = offset; i < array.Length && i - offset < count; i++)
			{
				if (i > 0)
				{
					stringBuilder.Append(", ");
				}
				object value = array.GetValue(i);
				if (value == null)
				{
					stringBuilder.Append("NULL");
				}
				else
				{
					stringBuilder.Append(value.ToString());
				}
			}
			stringBuilder.Append("]");
			return stringBuilder.ToString();
		}
	}
}

