namespace Photon.Client;

/// <summary>Build target framework supported by this dll.</summary>
public enum TargetFrameworks
{
	/// <summary>Compiled for some framework that wasn't properly defined with the other values.</summary>
	Unknown,
	/// <summary>Compiled for .Net 3.5. Obsolete.</summary>
	Net35,
	/// <summary>Compiled for .Net Standard2.0.</summary>
	NetStandard20,
	/// <summary>Compiled with NETFX_CORE define.</summary>
	Metro,
	/// <summary>Compiled for .Net Standard2.1.</summary>
	NetStandard21
}
