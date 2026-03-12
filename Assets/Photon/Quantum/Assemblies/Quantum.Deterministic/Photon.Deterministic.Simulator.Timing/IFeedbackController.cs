namespace Photon.Deterministic.Simulator.Timing
{
	internal interface IFeedbackController
	{
		double Output();

		void Update(double sample, double target, double dt);

		void Reset();

		void ResetOutput();
	}
}

