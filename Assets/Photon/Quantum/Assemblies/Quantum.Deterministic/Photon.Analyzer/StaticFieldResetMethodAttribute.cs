using System;
using System.Diagnostics;

namespace Photon.Analyzer
{
	[AttributeUsage(AttributeTargets.Method)]
	[Conditional("false")]
	public class StaticFieldResetMethodAttribute : Attribute
	{
	}
}

