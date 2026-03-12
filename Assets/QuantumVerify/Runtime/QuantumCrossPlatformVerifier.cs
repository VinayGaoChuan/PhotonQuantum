using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Photon.Deterministic;
using Quantum;
using UnityEngine;

public sealed class QuantumCrossPlatformVerifier : MonoBehaviour {
  private const int DefaultTickCount = 360;
  private const int ServiceSlice = 12;

  private readonly List<string> _lines = new();
  private readonly List<ScenarioDefinition> _scenarios = new();
  private Vector2 _scroll;
  private GUIStyle _labelStyle;
  private GUIStyle _buttonStyle;
  private bool _isRunning;
  private string _summary = "Idle";
  private Coroutine _runRoutine;

  private void Awake() {
    Application.targetFrameRate = 60;
    BuildScenarioCatalog();
  }

  private void Start() {
    StartRun();
  }

  private void OnGUI() {
    if (_labelStyle == null) {
      _labelStyle = new GUIStyle(GUI.skin.label) {
        fontSize = Mathf.Max(24, Screen.width / 42),
        richText = true,
        wordWrap = true,
        alignment = TextAnchor.UpperLeft
      };
      _buttonStyle = new GUIStyle(GUI.skin.button) {
        fontSize = Mathf.Max(22, Screen.width / 45),
        fixedHeight = Mathf.Max(72, Screen.height / 16f)
      };
    }

    GUILayout.BeginArea(new Rect(20, 20, Screen.width - 40, Screen.height - 40));
    GUILayout.Label($"Quantum Cross Platform Verify\n{_summary}", _labelStyle);
    GUILayout.Space(12);

    if (!_isRunning && GUILayout.Button("Run Again", _buttonStyle)) {
      StartRun();
    }

    GUILayout.Space(12);
    _scroll = GUILayout.BeginScrollView(_scroll);
    GUILayout.Label(string.Join("\n", _lines), _labelStyle);
    GUILayout.EndScrollView();
    GUILayout.EndArea();
  }

  private void StartRun() {
    if (_runRoutine != null) {
      StopCoroutine(_runRoutine);
    }

    _lines.Clear();
    _summary = "Preparing...";
    _runRoutine = StartCoroutine(RunAll());
  }

  private IEnumerator RunAll() {
    _isRunning = true;
    AppendLine("=== Pure Quantum Cross Platform Verify ===");
    AppendLine("Compare checksumDigest/stateDigest/finalState across Android/iOS.");

    RuntimeAssets runtimeAssets;
    try {
      QuantumUnityDB.UpdateGlobal();
      runtimeAssets = ResolveRuntimeAssets();
      AppendLine($"Map={runtimeAssets.MapPath}");
      AppendLine($"Simulation={runtimeAssets.SimulationPath}");
      AppendLine($"Systems={runtimeAssets.SystemsPath}");
    } catch (Exception ex) {
      AppendLine($"<color=red>BOOTSTRAP FAIL</color> {ex}");
      FinishRun();
      yield break;
    }

    yield return null;

    foreach (var scenario in _scenarios) {
      ScenarioResult result = null;
      yield return RunScenario(runtimeAssets, scenario, 1.0 / 60.0, r => result = r);
      AppendScenarioResult(scenario.Name, result);
    }

    yield return RunRepeatability(runtimeAssets);
    yield return RunCadenceCompare(runtimeAssets);

    AppendLine("=== End ===");
    AppendLine("If Android and iOS outputs are byte-for-byte equal, the kernel is stable for these cases.");
    FinishRun();
  }

  private IEnumerator RunRepeatability(RuntimeAssets runtimeAssets) {
    var scenario = FindScenario("weave-1p");
    AppendLine("[RUN] same-process-repeatability");

    ScenarioResult runA = null;
    ScenarioResult runB = null;

    yield return RunScenario(runtimeAssets, scenario, 1.0 / 60.0, r => runA = r);
    yield return RunScenario(runtimeAssets, scenario, 1.0 / 60.0, r => runB = r);

    if (!IsSuccess(runA) || !IsSuccess(runB)) {
      AppendLine("<color=red>FAIL</color> repeatability | scenario execution failed");
      yield break;
    }

    if (runA.ChecksumDigest == runB.ChecksumDigest && runA.StateDigest == runB.StateDigest && runA.FinalState == runB.FinalState) {
      AppendLine($"<color=lime>PASS</color> repeatability | checksumDigest={runA.ChecksumDigest} stateDigest={runA.StateDigest}");
    } else {
      AppendLine($"<color=red>FAIL</color> repeatability | A={runA.ChecksumDigest}/{runA.StateDigest} B={runB.ChecksumDigest}/{runB.StateDigest}");
    }
  }

  private IEnumerator RunCadenceCompare(RuntimeAssets runtimeAssets) {
    var scenario = FindScenario("dual-opposed-2p");
    AppendLine("[RUN] service-cadence-compare");

    ScenarioResult at60 = null;
    ScenarioResult at120 = null;
    ScenarioResult at30 = null;

    yield return RunScenario(runtimeAssets, scenario, 1.0 / 60.0, r => at60 = r);
    yield return RunScenario(runtimeAssets, scenario, 1.0 / 120.0, r => at120 = r);
    yield return RunScenario(runtimeAssets, scenario, 1.0 / 30.0, r => at30 = r);

    if (!IsSuccess(at60) || !IsSuccess(at120) || !IsSuccess(at30)) {
      AppendLine("<color=red>FAIL</color> cadence | one or more runs failed");
      yield break;
    }

    var passed = at60.ChecksumDigest == at120.ChecksumDigest &&
                 at60.ChecksumDigest == at30.ChecksumDigest &&
                 at60.StateDigest == at120.StateDigest &&
                 at60.StateDigest == at30.StateDigest &&
                 at60.FinalState == at120.FinalState &&
                 at60.FinalState == at30.FinalState;

    if (passed) {
      AppendLine($"<color=lime>PASS</color> cadence | checksumDigest={at60.ChecksumDigest} stateDigest={at60.StateDigest}");
    } else {
      AppendLine($"<color=red>FAIL</color> cadence | 60={at60.ChecksumDigest}/{at60.StateDigest} 120={at120.ChecksumDigest}/{at120.StateDigest} 30={at30.ChecksumDigest}/{at30.StateDigest}");
    }
  }

  private IEnumerator RunScenario(RuntimeAssets runtimeAssets, ScenarioDefinition scenario, double serviceStep, Action<ScenarioResult> onComplete) {
    SessionRunner runner = null;
    DynamicAssetDB dynamicDb = null;
    var callbackDispatcher = new CallbackDispatcher();
    var eventDispatcher = new EventDispatcher();
    var checksums = new List<ChecksumPoint>(256);
    var stateCheckpoints = new List<string>(32);
    var playersAdded = false;
    var lastObservedFrame = -1;
    ScenarioResult result = null;

    callbackDispatcher.SubscribeManual((CallbackPollInput callback) => {
      if (callback.IsInputSet) {
        return;
      }

      var input = new Quantum.Input();
      FillInputForFrame(scenario, callback.Frame, callback.PlayerSlot, ref input);
      callback.SetInput(input, DeterministicInputFlags.Repeatable);
    });

    callbackDispatcher.SubscribeManual((CallbackGameStarted callback) => {
      if (!callback.IsResync) {
        callback.Game.StartRecordingChecksums();
      }

      if (playersAdded) {
        return;
      }

      for (var i = 0; i < scenario.PlayerCount; i++) {
        callback.Game.AddPlayer(i, new RuntimePlayer {
          PlayerNickname = $"Verifier-{i + 1}"
        });
      }

      playersAdded = true;
    });

    callbackDispatcher.SubscribeManual((CallbackChecksumComputed callback) => {
      checksums.Add(new ChecksumPoint {
        Frame = callback.Frame,
        Checksum = callback.Checksum.ToString()
      });
    });

    try {
      dynamicDb = new DynamicAssetDB(new QuantumUnityNativeAllocator(), false);
      var args = new SessionRunner.Arguments {
        RunnerFactory = QuantumRunnerUnityFactory.DefaultFactory,
        GameParameters = QuantumRunnerUnityFactory.CreateGameParameters,
        CallbackDispatcher = callbackDispatcher,
        EventDispatcher = eventDispatcher,
        RuntimeConfig = runtimeAssets.CreateRuntimeConfig(scenario.Seed),
        SessionConfig = CreateSessionConfig(scenario.PlayerCount),
        GameMode = DeterministicGameMode.Local,
        PlayerCount = scenario.PlayerCount,
        RunnerId = $"PQVerify-{scenario.Name}",
        InitialDynamicAssets = dynamicDb,
        RecordingFlags = RecordingFlags.Checksums,
        DeltaTimeType = SimulationUpdateTime.EngineDeltaTime,
        GameFlags = QuantumGameFlags.DisableInterpolatableStates | QuantumGameFlags.DisableMemoryIntegrityCheck
      };

      runner = SessionRunner.Start(args);
    } catch (Exception ex) {
      result = new ScenarioResult {
        Error = ex.ToString()
      };
      CompleteScenario(runner, dynamicDb, onComplete, result);
      yield return null;
      yield break;
    }

    var waitCounter = 0;
    while ((runner.State != SessionRunner.SessionState.Running || !playersAdded) && waitCounter < 600) {
      runner.Service(serviceStep);
      QuantumUnityDB.UpdateGlobal();
      waitCounter++;
      if (waitCounter % ServiceSlice == 0) {
        _summary = $"Starting {scenario.Name}...";
        yield return null;
      }
    }

    if (runner.State != SessionRunner.SessionState.Running || !playersAdded) {
      result = new ScenarioResult {
        Error = $"Runner failed to enter running state for {scenario.Name}"
      };
      CompleteScenario(runner, dynamicDb, onComplete, result);
      yield return null;
      yield break;
    }

    while (runner.Session?.FramePredicted == null || runner.Session.FramePredicted.Number < scenario.TickCount) {
      try {
        runner.Service(serviceStep);
        QuantumUnityDB.UpdateGlobal();
      } catch (Exception ex) {
        result = new ScenarioResult {
          Error = ex.ToString()
        };
        CompleteScenario(runner, dynamicDb, onComplete, result);
        yield return null;
        yield break;
      }

      var predicted = runner.Session?.FramePredicted;
      if (predicted != null && predicted.Number != lastObservedFrame) {
        lastObservedFrame = predicted.Number;
        if (predicted.Number % 30 == 0) {
          stateCheckpoints.Add($"{predicted.Number}:{BuildStateSummary(predicted.GetSingleton<FrameSyncKernelState>())}");
        }
      }

      if (predicted != null && predicted.Number % ServiceSlice == 0) {
        _summary = $"Running {scenario.Name} frame={predicted.Number}/{scenario.TickCount}";
        yield return null;
      }
    }

    var finalFrame = runner.Session.FramePredicted;
    var finalState = finalFrame.GetSingleton<FrameSyncKernelState>();
    result = new ScenarioResult {
      FinalFrame = finalFrame.Number,
      FinalChecksum = checksums.Count > 0 ? checksums[checksums.Count - 1].Checksum : "0",
      ChecksumDigest = ComputeDigest(checksums),
      StateDigest = ComputeDigest(stateCheckpoints),
      FinalState = BuildStateSummary(finalState)
    };

    CompleteScenario(runner, dynamicDb, onComplete, result);
    yield return null;
  }

  private static void CompleteScenario(SessionRunner runner, DynamicAssetDB dynamicDb, Action<ScenarioResult> onComplete, ScenarioResult result) {
    if (runner != null) {
      runner.Shutdown();
      runner.Dispose();
    }

    dynamicDb?.Dispose();
    QuantumUnityDB.UpdateGlobal();
    onComplete?.Invoke(result);
  }

  private RuntimeAssets ResolveRuntimeAssets() {
    QuantumUnityDB.Entry mapEntry = null;
    QuantumUnityDB.Entry simulationEntry = null;
    QuantumUnityDB.Entry systemsEntry = null;

    foreach (var entry in QuantumUnityDB.Global.Entries) {
      if (entry?.Source?.AssetType == null) {
        continue;
      }

      var assetType = entry.Source.AssetType;
      if (assetType == typeof(Quantum.Map) || assetType.IsSubclassOf(typeof(Quantum.Map))) {
        if (entry.Path != null && entry.Path.Contains("Map001_Map")) {
          mapEntry = entry;
        } else {
          mapEntry ??= entry;
        }
      } else if (assetType == typeof(SimulationConfig) || assetType.IsSubclassOf(typeof(SimulationConfig))) {
        if (entry.Path != null && (entry.Path.Contains("DefaultConfigSimulation") || entry.Path.Contains("DefaultSimulationConfig"))) {
          simulationEntry = entry;
        } else {
          simulationEntry ??= entry;
        }
      } else if (assetType == typeof(SystemsConfig) || assetType.IsSubclassOf(typeof(SystemsConfig))) {
        if (entry.Path != null && (entry.Path.Contains("DefaultConfigSystems") || entry.Path.Contains("DefaultSystemsConfig"))) {
          systemsEntry = entry;
        } else {
          systemsEntry ??= entry;
        }
      }
    }

    if (mapEntry == null) {
      throw new InvalidOperationException("No Quantum.Map asset found in QuantumUnityDB.");
    }
    if (simulationEntry == null) {
      throw new InvalidOperationException("No SimulationConfig asset found in QuantumUnityDB.");
    }
    if (systemsEntry == null) {
      throw new InvalidOperationException("No SystemsConfig asset found in QuantumUnityDB.");
    }

    var map = QuantumUnityDB.GetGlobalAsset(mapEntry.Guid) as Quantum.Map;
    var simulationConfig = QuantumUnityDB.GetGlobalAsset(simulationEntry.Guid) as SimulationConfig;
    var systemsConfig = QuantumUnityDB.GetGlobalAsset(systemsEntry.Guid) as SystemsConfig;

    if (map == null) {
      throw new InvalidOperationException($"Failed to load map asset: {mapEntry.Path}");
    }
    if (simulationConfig == null) {
      throw new InvalidOperationException($"Failed to load simulation config: {simulationEntry.Path}");
    }
    if (systemsConfig == null) {
      throw new InvalidOperationException($"Failed to load systems config: {systemsEntry.Path}");
    }

    return new RuntimeAssets {
      Map = map,
      MapPath = mapEntry.Path,
      SimulationConfig = simulationConfig,
      SystemsConfig = systemsConfig,
      SimulationPath = simulationEntry.Path,
      SystemsPath = systemsEntry.Path
    };
  }

  private static DeterministicSessionConfig CreateSessionConfig(int playerCount) {
    var config = DeterministicSessionConfig.FromByteArray(DeterministicSessionConfig.ToByteArray(QuantumDeterministicSessionConfigAsset.DefaultConfig));
    config.PlayerCount = playerCount;
    config.InputDeltaCompression = true;
    config.ChecksumInterval = 1;
    config.LockstepSimulation = false;
    config.UpdateFPS = 60;
    return config;
  }

  private static void FillInputForFrame(ScenarioDefinition scenario, int frame, int playerSlot, ref Quantum.Input input) {
    for (var i = 0; i < scenario.InputWindows.Length; i++) {
      var window = scenario.InputWindows[i];
      if (window.PlayerIndex != playerSlot) {
        continue;
      }

      if (frame < window.StartFrame || frame > window.EndFrame) {
        continue;
      }

      input.EncodedMoveDirection = window.EncodedMoveDirection;
      input.ActionMask = window.ActionMask;
    }
  }

  private void AppendScenarioResult(string name, ScenarioResult result) {
    if (!IsSuccess(result)) {
      AppendLine($"<color=red>FAIL</color> {name} | {result?.Error ?? "null result"}");
      return;
    }

    AppendLine($"<color=lime>OK</color> {name}");
    AppendLine($"  checksumDigest={result.ChecksumDigest}");
    AppendLine($"  stateDigest={result.StateDigest}");
    AppendLine($"  finalChecksum={result.FinalChecksum} finalFrame={result.FinalFrame}");
    AppendLine($"  finalState={result.FinalState}");
  }

  private static string BuildStateSummary(FrameSyncKernelState state) {
    return $"tick={state.Tick} p0={state.P0Position} v0={state.P0Velocity} e0={state.P0Energy} p1={state.P1Position} v1={state.P1Velocity} e1={state.P1Energy} dist={state.LastDistance} acc={state.Accumulator} bounce={state.BounceCount} action={state.ActionCount}";
  }

  private static string ComputeDigest(List<ChecksumPoint> values) {
    var builder = new StringBuilder(values.Count * 32);
    for (var i = 0; i < values.Count; i++) {
      builder.Append(values[i].Frame);
      builder.Append(':');
      builder.Append(values[i].Checksum);
      builder.Append(';');
    }
    return ComputeDigest(builder.ToString());
  }

  private static string ComputeDigest(List<string> values) {
    var builder = new StringBuilder(values.Count * 48);
    for (var i = 0; i < values.Count; i++) {
      builder.Append(values[i]);
      builder.Append(';');
    }
    return ComputeDigest(builder.ToString());
  }

  private static string ComputeDigest(string value) {
    using var sha = SHA256.Create();
    var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(value));
    var builder = new StringBuilder(hash.Length * 2);
    for (var i = 0; i < hash.Length; i++) {
      builder.Append(hash[i].ToString("x2"));
    }
    return builder.ToString();
  }

  private ScenarioDefinition FindScenario(string name) {
    for (var i = 0; i < _scenarios.Count; i++) {
      if (_scenarios[i].Name == name) {
        return _scenarios[i];
      }
    }

    throw new InvalidOperationException($"Scenario not found: {name}");
  }

  private static bool IsSuccess(ScenarioResult result) {
    return result != null && string.IsNullOrEmpty(result.Error);
  }

  private void BuildScenarioCatalog() {
    _scenarios.Clear();
    _scenarios.Add(new ScenarioDefinition("idle-1p", 1, 12345));
    _scenarios.Add(new ScenarioDefinition("move-1p", 1, 12345,
      new InputWindow(1, 120, 0, 1, 0),
      new InputWindow(121, 220, 0, 3, 1),
      new InputWindow(221, 300, 0, 5, 0)));
    _scenarios.Add(new ScenarioDefinition("weave-1p", 1, 22334,
      new InputWindow(1, 45, 0, 2, 0),
      new InputWindow(46, 90, 0, 4, 1),
      new InputWindow(91, 135, 0, 6, 0),
      new InputWindow(136, 180, 0, 8, 1),
      new InputWindow(181, 225, 0, 1, 0),
      new InputWindow(226, 270, 0, 3, 1),
      new InputWindow(271, 315, 0, 5, 0),
      new InputWindow(316, 360, 0, 7, 1)));
    _scenarios.Add(new ScenarioDefinition("dual-opposed-2p", 2, 34567,
      new InputWindow(1, 100, 0, 3, 1),
      new InputWindow(1, 100, 1, 7, 0),
      new InputWindow(101, 180, 0, 2, 0),
      new InputWindow(101, 180, 1, 6, 1),
      new InputWindow(181, 260, 0, 4, 1),
      new InputWindow(181, 260, 1, 8, 0),
      new InputWindow(261, 360, 0, 5, 0),
      new InputWindow(261, 360, 1, 1, 1)));
  }

  private void AppendLine(string line) {
    _lines.Add($"[{DateTime.Now:HH:mm:ss}] {line}");
    if (_lines.Count > 240) {
      _lines.RemoveAt(0);
    }
  }

  private void FinishRun() {
    _summary = "Done";
    _isRunning = false;
    _runRoutine = null;
  }

  private readonly struct InputWindow {
    public InputWindow(int startFrame, int endFrame, int playerIndex, byte encodedMoveDirection, byte actionMask) {
      StartFrame = startFrame;
      EndFrame = endFrame;
      PlayerIndex = playerIndex;
      EncodedMoveDirection = encodedMoveDirection;
      ActionMask = actionMask;
    }

    public int StartFrame { get; }
    public int EndFrame { get; }
    public int PlayerIndex { get; }
    public byte EncodedMoveDirection { get; }
    public byte ActionMask { get; }
  }

  private sealed class ScenarioDefinition {
    public ScenarioDefinition(string name, int playerCount, int seed, params InputWindow[] inputWindows) {
      Name = name;
      PlayerCount = playerCount;
      Seed = seed;
      TickCount = DefaultTickCount;
      InputWindows = inputWindows ?? Array.Empty<InputWindow>();
    }

    public string Name { get; }
    public int PlayerCount { get; }
    public int Seed { get; }
    public int TickCount { get; }
    public InputWindow[] InputWindows { get; }
  }

  private sealed class RuntimeAssets {
    public Quantum.Map Map;
    public string MapPath;
    public SimulationConfig SimulationConfig;
    public SystemsConfig SystemsConfig;
    public string SimulationPath;
    public string SystemsPath;

    public RuntimeConfig CreateRuntimeConfig(int seed) {
      return new RuntimeConfig {
        Seed = seed,
        Map = Map,
        SimulationConfig = SimulationConfig,
        SystemsConfig = SystemsConfig
      };
    }
  }

  private sealed class ScenarioResult {
    public string Error;
    public int FinalFrame;
    public string FinalChecksum;
    public string ChecksumDigest;
    public string StateDigest;
    public string FinalState;
  }

  private struct ChecksumPoint {
    public int Frame;
    public string Checksum;
  }
}
