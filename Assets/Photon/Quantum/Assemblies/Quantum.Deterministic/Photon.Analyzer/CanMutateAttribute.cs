using System;

namespace Photon.Analyzer
{
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
	public class CanMutateAttribute : Attribute
	{
	}
}

