namespace Photon.Deterministic.Simulator.Timing
{
	/// <summary>
	/// Reduces errors at a fixed rate using an on-off controller.
	/// </summary>
	internal class OnOff : IFeedbackController
	{
		private readonly double _outputMin;

		private readonly double _outputMax;

		private readonly double _deadzoneMin;

		private readonly double _deadzoneMax;

		private double _output;

		public OnOff(double outputMin, double outputMax, double deadzoneMin, double deadzoneMax)
		{
			_outputMin = outputMin;
			_outputMax = outputMax;
			_deadzoneMin = deadzoneMin;
			_deadzoneMax = deadzoneMax;
			_output = 0.0;
		}

		double IFeedbackController.Output()
		{
			return _output;
		}

		void IFeedbackController.Update(double sample, double target, double dt)
		{
			double num = target - sample;
			if (num > _deadzoneMax)
			{
				_output = _outputMax;
			}
			else if (num < _deadzoneMin)
			{
				_output = _outputMin;
			}
			else
			{
				_output = 0.0;
			}
		}

		void IFeedbackController.Reset()
		{
			_output = 0.0;
		}

		void IFeedbackController.ResetOutput()
		{
			_output = 0.0;
		}
	}
}

