namespace Quantum {
  public static partial class QuantumLogInitializer {
    static partial void InitializeUser(ref LogLevel logLevel, ref TraceChannels traceChannels) {
      if (global::Quantum.Log.IsInitialized) {
        return;
      }

      global::Quantum.Log.Init(
        error: msg => ZLog.LogError(msg, "Quantum"),
        warn: msg => ZLog.LogWarning(msg, "Quantum"),
        info: msg => ZLog.LogMobile(msg, "Quantum"),
        exn: ex => ZLog.LogException(ex, "Quantum"));
    }
  }
}
