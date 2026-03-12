namespace Quantum {
  public static partial class QuantumLogInitializer {
    static partial void InitializeUser(ref LogLevel logLevel, ref TraceChannels traceChannels) {
      if (global::Quantum.Log.IsInitialized) {
        return;
      }

      global::Quantum.Log.Init(
        error: msg => ZLog.LogErrorChannel(msg, "Quantum"),
        warn: msg => ZLog.LogWarningChannel(msg, "Quantum"),
        info: msg => ZLog.Log(msg, "Quantum"),
        exn: ex => ZLog.LogExceptionChannel(ex, "Quantum"));
    }
  }
}
