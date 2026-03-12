namespace Photon.Deterministic
{
	internal interface ITimeProvider
	{
		void Initialize(DeterministicSessionConfig config);

		void Reset(int verifiedFrame, double roundTripTime, double serverTime, double serverTimeScale);

		void OnRttUpdated(double roundTripTime);

		void OnVerifiedFrameReceived(int verifiedFrame);

		void OnClockSyncMessageReceived(double serverTime, double serverTimeScale);

		void Update(double unscaledDeltaTime);

		Time GetTime();
	}
}

