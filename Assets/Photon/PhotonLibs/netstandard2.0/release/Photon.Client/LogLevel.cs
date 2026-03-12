using System;

namespace Photon.Client;

/// <summary>
/// Level / amount of DebugReturn callbacks. Each debug level includes output for lower ones: OFF, ERROR, WARNING, INFO, ALL.
/// </summary>
public enum LogLevel : byte
{
	/// <summary>No debug out.</summary>
	Off = 0,
	/// <summary>No debug out.</summary>
	[Obsolete]
	OFF = 0,
	/// <summary>Only error descriptions.</summary>
	Error = 1,
	/// <summary>Only error descriptions.</summary>
	[Obsolete]
	ERROR = 1,
	/// <summary>Warnings and errors.</summary>
	Warning = 2,
	/// <summary>Warnings and errors.</summary>
	[Obsolete]
	WARNING = 2,
	/// <summary>Information about internal workflows, warnings and errors.</summary>
	Info = 3,
	/// <summary>Information about internal workflows, warnings and errors.</summary>
	[Obsolete]
	INFO = 3,
	/// <summary>Most complete workflow description (but lots of debug output), info, warnings and errors.</summary>
	Debug = 4,
	/// <summary>Most complete workflow description (but lots of debug output), info, warnings and errors.</summary>
	[Obsolete]
	ALL = 4
}
