namespace Photon.Deterministic
{
	internal struct Time
	{
		internal double _inputTime;

		internal double _simulationTime;

		internal double _interpTime;

		public readonly double InputTime => _inputTime;

		public readonly double SimulationTime => _simulationTime;

		public readonly double InterpolationTime => _interpTime;
	}
}

