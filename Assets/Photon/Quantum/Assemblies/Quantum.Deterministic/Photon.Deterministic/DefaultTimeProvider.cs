using System;
using System.Diagnostics;
using Photon.Deterministic.Simulator.Timing;

namespace Photon.Deterministic
{
	internal class DefaultTimeProvider : ITimeProvider
	{
		private TimeSyncSettings _config;

		private IFeedbackController _clockFeedback;

		private double _targetInputArrivalOffset;

		private double _clockTimeScaleOffset;

		private IFeedbackController _delayFeedback;

		private double _targetInputDelay;

		private double _delayTimeScaleOffset;

		private IFeedbackController _interpFeedback;

		private double _targetInterpDelay;

		private double _interpTimeScaleOffset;

		private double _inputTime;

		private double _simulationTime;

		private double _interpTime;

		private TimeSeries _roundTripTime;

		private TimeSeries _inputArrivalOffset;

		private Stopwatch _verifiedFrameTimer;

		private TimeSeries _verifiedFrameTimeDelta;

		private int _verifiedFrame;

		private Stopwatch _clockSyncTimer;

		private double _lastSeenServerTime;

		private double _lastSeenServerTimeScale;

		private Stopwatch _resetInputTimer;

		private Stopwatch _resetInterpTimer;

		private bool _isZeroLag = true;

		private bool _isSpectating;

		private bool IsZeroLag
		{
			get
			{
				return _isZeroLag;
			}
			set
			{
				_isZeroLag = value;
			}
		}

		private bool IsSpectating
		{
			get
			{
				return _isSpectating;
			}
			set
			{
				_isSpectating = value;
			}
		}

		internal DefaultTimeProvider(DeterministicSessionConfig config)
		{
			Initialize(config);
		}

		private void Initialize(DeterministicSessionConfig config)
		{
			double num = 1.0 / (double)config.UpdateFPS;
			_config = default(TimeSyncSettings);
			_config.simDeltaTime = num;
			_config.inputDelayMin = (double)config.InputDelayMin * num;
			_config.inputDelayMax = (double)config.InputDelayMax * num;
			_config.predictionMax = (double)config.InputDelayPingStart / 1000.0;
			_config.timeScaleOffsetMax = 0.01;
			_config.lateTolerance = 0.01;
			_config.extraBufferedTicks = 0.0;
			double timeScaleOffsetMax = _config.timeScaleOffsetMax;
			_clockFeedback = new OnOff(0.0 - timeScaleOffsetMax, timeScaleOffsetMax, -0.025, 0.025);
			_delayFeedback = new OnOff(0.0 - timeScaleOffsetMax, timeScaleOffsetMax, -0.025, 0.025);
			_interpFeedback = new OnOff(0.0 - timeScaleOffsetMax, timeScaleOffsetMax, -0.025, 0.025);
			_clockSyncTimer = new Stopwatch();
			_verifiedFrameTimer = new Stopwatch();
			_resetInputTimer = new Stopwatch();
			_resetInterpTimer = new Stopwatch();
			_roundTripTime = new TimeSeries(config.TimeCorrectionRate);
			_inputArrivalOffset = new TimeSeries(config.TimeCorrectionRate);
			_verifiedFrameTimeDelta = new TimeSeries(config.UpdateFPS);
		}

		private void Reset(int verifiedFrame, double roundTripTime, double serverTime, double serverTimeScale)
		{
			_clockFeedback.Reset();
			_delayFeedback.Reset();
			_interpFeedback.Reset();
			_roundTripTime.Clear();
			_inputArrivalOffset.Clear();
			_verifiedFrameTimeDelta.Clear();
			_clockSyncTimer.Reset();
			_verifiedFrameTimer.Reset();
			_resetInputTimer.Reset();
			_resetInterpTimer.Reset();
			UpdateVerifiedFrame(verifiedFrame);
			UpdateRtt(roundTripTime);
			UpdateServerTime(serverTime, serverTimeScale);
			ResetInputAndSimulationTime();
			ResetInterpolationTime();
		}

		private void ResetInputAndSimulationTime()
		{
			double num = _roundTripTime.Smoothed(0.5);
			_clockFeedback.Reset();
			_clockTimeScaleOffset = 0.0;
			double num2 = _lastSeenServerTime + (_clockSyncTimer.Elapsed.TotalSeconds + num) * _lastSeenServerTimeScale;
			_inputTime = num2 + _targetInputArrivalOffset;
			_delayFeedback.Reset();
			_delayTimeScaleOffset = 0.0;
			_simulationTime = _inputTime - _targetInputDelay;
		}

		private void ResetInterpolationTime()
		{
			_interpFeedback.Reset();
			_interpTimeScaleOffset = 0.0;
			_interpTime = (double)_verifiedFrame * _config.simDeltaTime + _verifiedFrameTimer.Elapsed.TotalSeconds - _targetInterpDelay;
		}

		private void UpdateRtt(double roundTripTime)
		{
			_roundTripTime.Add(roundTripTime);
			UpdateOutgoingTargets();
		}

		private void UpdateServerTime(double serverTime, double serverTimeScale)
		{
			_lastSeenServerTime = Math.Max(_lastSeenServerTime, serverTime);
			_lastSeenServerTimeScale = serverTimeScale;
			_clockSyncTimer.Restart();
			double value = CalculateInputArrivalOffset();
			_inputArrivalOffset.Add(value);
			UpdateOutgoingTargets();
		}

		private void UpdateVerifiedFrame(int verifiedFrame)
		{
			_verifiedFrame = verifiedFrame;
			double totalSeconds = _verifiedFrameTimer.Elapsed.TotalSeconds;
			_verifiedFrameTimeDelta.Add(totalSeconds);
			_verifiedFrameTimer.Restart();
			UpdateIncomingTargets();
		}

		private double CalculateInputArrivalOffset()
		{
			double num = _roundTripTime.Smoothed(0.5);
			double inputTime = _inputTime;
			double num2 = _lastSeenServerTime + (_clockSyncTimer.Elapsed.TotalSeconds + num) * _lastSeenServerTimeScale;
			return inputTime - num2;
		}

		private void UpdateOutgoingTargets()
		{
			double num = _roundTripTime.Smoothed(0.5);
			double num2 = Math.Max(0.0, num - _config.predictionMax - _config.inputDelayMin);
			_targetInputDelay = (_config.inputDelayMin + num2) * _lastSeenServerTimeScale;
			double targetInputArrivalOffset;
			if (IsZeroLag)
			{
				_targetInputArrivalOffset = _targetInputDelay - num * 0.5;
				targetInputArrivalOffset = 0.0 - _config.predictionMax * 0.5;
			}
			else
			{
				_targetInputArrivalOffset = 0.0;
				targetInputArrivalOffset = 0.0;
			}
			_targetInputArrivalOffset += TimeSeries.InverseCdfNormal(1.0 - _config.lateTolerance) * _inputArrivalOffset.Dev;
			_targetInputArrivalOffset += _config.extraBufferedTicks * _config.simDeltaTime;
			if (IsSpectating)
			{
				_targetInputArrivalOffset = targetInputArrivalOffset;
				_targetInputArrivalOffset -= num * 0.5;
			}
		}

		private void UpdateIncomingTargets()
		{
			_targetInterpDelay = TimeSeries.InverseCdfNormal(1.0 - _config.lateTolerance) * _verifiedFrameTimeDelta.Dev;
			_targetInterpDelay += _config.extraBufferedTicks * _config.simDeltaTime;
		}

		private void Update(double unscaledDeltaTime)
		{
			double num = CalculateInputArrivalOffset();
			double num2 = Math.Max(0.25, Math.Min(1.0, _targetInputArrivalOffset));
			double num3 = num - _targetInputArrivalOffset;
			if (_resetInputTimer.IsRunning)
			{
				if (_resetInputTimer.Elapsed.TotalSeconds > 1.0)
				{
					_resetInputTimer.Reset();
				}
			}
			else if (num3 > num2)
			{
				ResetInputAndSimulationTime();
				_resetInputTimer.Start();
			}
			_clockFeedback.Update(num, _targetInputArrivalOffset, unscaledDeltaTime);
			_clockTimeScaleOffset = _clockFeedback.Output();
			double sample = _inputTime - _simulationTime;
			_delayFeedback.Update(sample, _targetInputDelay, unscaledDeltaTime);
			_delayTimeScaleOffset = _delayFeedback.Output();
			double num4 = (double)_verifiedFrame * _config.simDeltaTime + _verifiedFrameTimer.Elapsed.TotalSeconds - _interpTime;
			double num5 = Math.Max(0.25, Math.Min(1.0, _targetInterpDelay));
			double num6 = num4 - _targetInterpDelay;
			if (_resetInterpTimer.IsRunning)
			{
				if (_resetInterpTimer.Elapsed.TotalSeconds > 1.0)
				{
					_resetInterpTimer.Reset();
				}
			}
			else if (num6 > num5)
			{
				ResetInterpolationTime();
				_resetInterpTimer.Start();
			}
			_interpFeedback.Update(num4, _targetInterpDelay, unscaledDeltaTime);
			_interpTimeScaleOffset = 0.0 - _interpFeedback.Output();
			_inputTime += (_lastSeenServerTimeScale + _clockTimeScaleOffset) * unscaledDeltaTime;
			_simulationTime += (_lastSeenServerTimeScale + _clockTimeScaleOffset + _delayTimeScaleOffset) * unscaledDeltaTime;
			_interpTime += (_lastSeenServerTimeScale + _interpTimeScaleOffset) * unscaledDeltaTime;
		}

		void ITimeProvider.Initialize(DeterministicSessionConfig config)
		{
			Initialize(config);
		}

		void ITimeProvider.Reset(int verifiedFrame, double roundTripTime, double serverTime, double serverTimeScale)
		{
			Reset(verifiedFrame, roundTripTime, serverTime, serverTimeScale);
		}

		void ITimeProvider.OnRttUpdated(double roundTripTime)
		{
			UpdateRtt(roundTripTime);
		}

		void ITimeProvider.OnVerifiedFrameReceived(int verifiedFrame)
		{
			UpdateVerifiedFrame(verifiedFrame);
		}

		void ITimeProvider.OnClockSyncMessageReceived(double serverTime, double serverTimeScale)
		{
			UpdateServerTime(serverTime, serverTimeScale);
		}

		void ITimeProvider.Update(double unscaledDeltaTime)
		{
			Update(unscaledDeltaTime);
		}

		Time ITimeProvider.GetTime()
		{
			return new Time
			{
				_inputTime = _inputTime,
				_simulationTime = _simulationTime,
				_interpTime = _interpTime
			};
		}
	}
}

