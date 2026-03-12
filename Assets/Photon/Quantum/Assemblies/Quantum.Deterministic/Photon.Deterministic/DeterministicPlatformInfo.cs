namespace Photon.Deterministic
{
	/// <summary>
	/// Essential platform dependent information and implementations.
	/// </summary>
	public class DeterministicPlatformInfo
	{
		/// <summary>
		/// Processor architectures.
		/// </summary>
		public enum Architectures
		{
			/// <summary>
			/// Apple ARMv7 family.
			/// </summary>
			ARMv7,
			/// <summary>
			/// Apple ARM64 family.
			/// </summary>
			ARM64,
			/// <summary>
			/// Intel and AMD x86 family.
			/// </summary>
			x86
		}

		/// <summary>
		/// Runtime information.
		/// </summary>
		public enum Runtimes
		{
			/// <summary>
			/// .Net Framework
			/// </summary>
			NetFramework,
			/// <summary>
			/// .Net Core
			/// </summary>
			NetCore,
			/// <summary>
			/// Unity Mono
			/// </summary>
			Mono,
			/// <summary>
			/// Unity IL2CPP
			/// </summary>
			IL2CPP
		}

		/// <summary>
		/// Runtime host information.
		/// </summary>
		public enum RuntimeHosts
		{
			/// <summary>
			/// Unity build
			/// </summary>
			Unity,
			/// <summary>
			/// Unity Editor
			/// </summary>
			UnityEditor,
			/// <summary>
			/// PhotonServer
			/// </summary>
			PhotonServer,
			/// <summary>
			/// .Net application
			/// </summary>
			NetApplication
		}

		/// <summary>
		/// Platform information
		/// </summary>
		public enum Platforms
		{
			/// <summary>
			/// Windows
			/// </summary>
			Windows,
			/// <summary>
			/// Mac
			/// </summary>
			OSX,
			/// <summary>
			/// Linux
			/// </summary>
			Linux,
			/// <summary>
			/// iOS
			/// </summary>
			IOS,
			/// <summary>
			/// Android
			/// </summary>
			Android,
			/// <summary>
			/// Xbox
			/// </summary>
			XboxOne,
			/// <summary>
			/// PlayStation
			/// </summary>
			PlayStation4,
			/// <summary>
			/// Nintendo Switch
			/// </summary>
			Switch,
			/// <summary>
			/// Apple T
			/// </summary>
			TVOS,
			/// <summary>
			/// WebGL
			/// </summary>
			WebGL
		}

		/// <summary>
		/// The processor architecture.
		/// </summary>
		public Architectures Architecture;

		/// <summary>
		/// The OS platform.
		/// </summary>
		public Platforms Platform;

		/// <summary>
		/// The runtime host the simulation is started from.
		/// </summary>
		public RuntimeHosts RuntimeHost;

		/// <summary>
		/// The runtime information.
		/// </summary>
		public Runtimes Runtime;

		/// <summary>
		/// The native memory allocator.
		/// </summary>
		public Native.Allocator Allocator;

		/// <summary>
		/// The number of cores to use.
		/// </summary>
		public int CoreCount;

		/// <summary>
		/// The platform dependend Quantum task runner.
		/// </summary>
		public IDeterministicPlatformTaskRunner TaskRunner;

		/// <summary>
		/// Create a debug string of the content.
		/// </summary>
		/// <returns>Debug string</returns>
		public override string ToString()
		{
			return string.Format("{0}: {1}, {2}: {3}, {4}: {5}, {6}: {7}, {8}: {9}, {10}: {11}", "Architecture", Architecture, "Platform", Platform, "RuntimeHost", RuntimeHost, "Runtime", Runtime, "Allocator", Allocator, "CoreCount", CoreCount);
		}
	}
}

