using System;
using System.Reflection;

namespace Photon.Deterministic
{
	/// <summary>
	/// The assembly attribute to specify the deterministic protocol version.
	/// </summary>
	[AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = true)]
	public sealed class DeterministicProtocolVersionAttribute : Attribute
	{
		/// <summary>
		/// The latest Quantum protocol version.
		/// </summary>
		public const string LATEST = "3.0.0.0";

		private string _version;

		private string[] _compatible;

		/// <summary>
		/// Get the assigned version.
		/// </summary>
		public string Version => _version;

		/// <summary>
		/// Return a list of the compatible versions.
		/// </summary>
		public string[] Compatible => _compatible;

		/// <summary>
		/// Create the attribute with the given version and compatible versions.
		/// </summary>
		/// <param name="version">Current version</param>
		/// <param name="compatible">Backwards compatible versions</param>
		public DeterministicProtocolVersionAttribute(string version, params string[] compatible)
		{
			_version = version;
			_compatible = compatible;
		}

		/// <summary>
		/// A helper method to get the attribute from an assembly.
		/// </summary>
		/// <param name="asm">Assembly to search for the attribute</param>
		/// <returns>An instance of the protocol version attribute if found.</returns>
		public static DeterministicProtocolVersionAttribute Get(Assembly asm)
		{
			return (DeterministicProtocolVersionAttribute)Attribute.GetCustomAttribute(asm, typeof(DeterministicProtocolVersionAttribute), inherit: false);
		}

		/// <summary>
		/// Convert a string to the protocol version enum.
		/// </summary>
		/// <param name="version">Version as string, default format is 1.0.0.0</param>
		/// <returns></returns>
		public static DeterministicProtocolVersions GetEnum(string version)
		{
			return version switch
			{
				"1.2.0.0" => DeterministicProtocolVersions.V1_2_0_0, 
				"1.2.1.0" => DeterministicProtocolVersions.V1_2_1_0, 
				"1.2.2.0" => DeterministicProtocolVersions.V1_2_2_0, 
				"1.2.3.0" => DeterministicProtocolVersions.V1_2_3_0, 
				"1.2.3.1" => DeterministicProtocolVersions.V1_2_3_1, 
				"2.0.0.0" => DeterministicProtocolVersions.V2_0_0_0, 
				"2.1.0.0" => DeterministicProtocolVersions.V2_1_0_0, 
				"2.2.0.0" => DeterministicProtocolVersions.V2_2_0_0, 
				"3.0.0.0" => DeterministicProtocolVersions.V3_0_0_0, 
				_ => DeterministicProtocolVersions.V1_2_0_0, 
			};
		}
	}
}

