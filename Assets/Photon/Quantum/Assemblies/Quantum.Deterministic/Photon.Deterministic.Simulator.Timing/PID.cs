namespace Photon.Deterministic.Simulator.Timing
{
	/// <summary>
	/// Reduces errors at a variable rate using a PID controller.
	/// </summary>
	internal class PID : IFeedbackController
	{
		private readonly double _Kp;

		private readonly double _Ki;

		private readonly double _Kd;

		private readonly double _outputMin;

		private readonly double _outputMax;

		private double _lastSample;

		private double _sum;

		private double _output;

		public PID(double Kp, double Ki, double Kd, double outputMin, double outputMax)
		{
			_Kp = Kp;
			_Ki = _Kp * Ki;
			_Kd = _Kp * Kd;
			_outputMin = outputMin;
			_outputMax = outputMax;
			_lastSample = 0.0;
			_sum = 0.0;
			_output = 0.0;
		}

		double IFeedbackController.Output()
		{
			return _output;
		}

		void IFeedbackController.Update(double sample, double target, double dt)
		{
			double num = target - sample;
			double num2 = sample - _lastSample;
			_output = _Kp * num;
			if (_output > _outputMax)
			{
				_output = _outputMax;
			}
			else if (_output < _outputMin)
			{
				_output = _outputMin;
			}
			_sum += _Ki * dt * num - _Kd / dt * num2;
			_output += _sum;
			if (_output > _outputMax)
			{
				_sum -= _output - _outputMax;
				_output = _outputMax;
			}
			else if (_output < _outputMin)
			{
				_sum += _outputMin - _output;
				_output = _outputMin;
			}
		}

		void IFeedbackController.Reset()
		{
			_lastSample = 0.0;
			_sum = 0.0;
			_output = 0.0;
		}

		void IFeedbackController.ResetOutput()
		{
			_output = 0.0;
		}
	}
}

