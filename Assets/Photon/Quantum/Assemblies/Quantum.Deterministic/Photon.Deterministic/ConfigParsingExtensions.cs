using System.Collections.Generic;

namespace Photon.Deterministic
{
	/// <summary>
	/// Used by the server to parse the config information
	/// </summary>
	public static class ConfigParsingExtensions
	{
		public static bool TryParseBool(this Dictionary<string, string> config, string key, ref bool result)
		{
			if (config.TryGetValue(key, out var value) && bool.TryParse(value, out var result2))
			{
				result = result2;
				return true;
			}
			return false;
		}

		public static bool TryParseBool(this Dictionary<string, string> config, string key, out bool result, bool defaultValue)
		{
			result = defaultValue;
			if (config.TryGetValue(key, out var value) && bool.TryParse(value, out var result2))
			{
				result = result2;
				return true;
			}
			return false;
		}

		public static bool TryParseInt(this Dictionary<string, string> config, string key, ref int result)
		{
			if (config.TryGetValue(key, out var value) && int.TryParse(value, out var result2))
			{
				result = result2;
				return true;
			}
			return false;
		}

		public static bool TryParseInt(this Dictionary<string, string> config, string key, out int result, int defaultValue)
		{
			result = defaultValue;
			if (config.TryGetValue(key, out var value) && int.TryParse(value, out var result2))
			{
				result = result2;
				return true;
			}
			return false;
		}

		public static bool TryGetString(this Dictionary<string, string> config, string key, out string result, string defaultValue)
		{
			result = defaultValue;
			if (config.TryGetValue(key, out var value) && !string.IsNullOrEmpty(value))
			{
				result = value;
				return true;
			}
			return false;
		}
	}
}

