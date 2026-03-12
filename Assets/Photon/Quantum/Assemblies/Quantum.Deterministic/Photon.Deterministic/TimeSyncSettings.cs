namespace Photon.Deterministic
{
	internal struct TimeSyncSettings
	{
		public double lateTolerance;

		public double extraBufferedTicks;

		public double timeScaleOffsetMax;

		public double predictionMax;

		public double inputDelayMin;

		public double inputDelayMax;

		public double simDeltaTime;
	}
}

