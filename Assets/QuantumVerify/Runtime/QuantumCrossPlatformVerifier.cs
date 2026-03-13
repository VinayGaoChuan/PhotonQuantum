using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Photon.Deterministic;
using Quantum;
using UnityEngine;

public sealed unsafe class QuantumCrossPlatformVerifier : MonoBehaviour {
  private const int DefaultTickCount = 360;
  private const int ServiceSlice = 12;
  private const int FinalFrameOffset = 1;
  private static readonly int[] StateDigestFrames = { 60, 120, 180, 240, 300, 360 };
  private const int ScreenLineLimit = 24;
  private const int ScreenHashLength = 12;

  private readonly List<string> _lines = new();
  private readonly List<string> _screenLines = new();
  private readonly List<ScenarioDefinition> _scenarios = new();
  private Vector2 _scroll;
  private GUIStyle _labelStyle;
  private GUIStyle _buttonStyle;
  private bool _isRunning;
  private string _summary = "待机";
  private Coroutine _runRoutine;
  private static readonly Regex RichTextRegex = new Regex("<.*?>", RegexOptions.Compiled);

  private void Awake() {
    ZLog.EnsureInitialized();
    Application.targetFrameRate = 60;
    BuildScenarioCatalog();
  }

  private void Start() {
    StartRun();
  }

  private void OnGUI() {
    if (_labelStyle == null) {
      _labelStyle = new GUIStyle(GUI.skin.label) {
        fontSize = Mathf.Max(42, Screen.width / 22),
        richText = true,
        wordWrap = true,
        alignment = TextAnchor.UpperLeft
      };
      _buttonStyle = new GUIStyle(GUI.skin.button) {
        fontSize = Mathf.Max(38, Screen.width / 24),
        fixedHeight = Mathf.Max(114, Screen.height / 11f)
      };
    }

    GUILayout.BeginArea(new Rect(20, 20, Screen.width - 40, Screen.height - 40));
    GUILayout.Label($"Quantum 跨平台校验\n{_summary}", _labelStyle);
    GUILayout.Space(12);

    if (!_isRunning && GUILayout.Button("重新运行", _buttonStyle)) {
      StartRun();
    }

    GUILayout.Space(12);
    _scroll = GUILayout.BeginScrollView(_scroll);
    GUILayout.Label(string.Join("\n", _screenLines), _labelStyle);
    GUILayout.EndScrollView();
    GUILayout.EndArea();
  }

  private void StartRun() {
    if (_runRoutine != null) {
      StopCoroutine(_runRoutine);
    }

    _lines.Clear();
    _screenLines.Clear();
    _summary = "准备中...";
    _runRoutine = StartCoroutine(RunAll());
  }

  private IEnumerator RunAll() {
    _isRunning = true;
    AppendLine("=== Pure Quantum Cross Platform Verify ===", showOnScreen: false);
    AppendLine("Compare checksumDigest/stateDigest/finalState across Android/iOS.", showOnScreen: false);
    AppendScreenLine("Quantum 跨平台校验");
    AppendScreenLine("安卓和 iOS 对比下面这些值。");
    EnsureRequiredQuantumGlobals();

    RuntimeAssets runtimeAssets = null;
    var bootstrapFailed = false;
    try {
      QuantumUnityDB.UpdateGlobal();
      runtimeAssets = ResolveRuntimeAssets();
      AppendLine($"Map={runtimeAssets.MapPath}", showOnScreen: false);
      AppendLine($"Simulation={runtimeAssets.SimulationPath}", showOnScreen: false);
      AppendLine($"Systems={runtimeAssets.SystemsPath}", showOnScreen: false);
      AppendScreenLine("启动成功");
    } catch (Exception ex) {
      ZLog.LogException(ex);
      AppendLine($"<color=red>BOOTSTRAP FAIL</color> {ex}", showOnScreen: false);
      AppendScreenLine("启动失败");
      AppendScreenLine($"异常: {ex.GetType().Name}");
      FinishRun();
      bootstrapFailed = true;
    }

    if (bootstrapFailed) {
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

    AppendLine("=== End ===", showOnScreen: false);
    AppendLine("If Android and iOS outputs are byte-for-byte equal, the kernel is stable for these cases.", showOnScreen: false);
    AppendScreenLine("安卓和 iOS 完全一致 = 通过");
    FinishRun();
  }

  private IEnumerator RunRepeatability(RuntimeAssets runtimeAssets) {
    var scenario = FindScenario("weave-1p");
    AppendLine("[RUN] same-process-repeatability", showOnScreen: false);

    ScenarioResult runA = null;
    ScenarioResult runB = null;

    yield return RunScenario(runtimeAssets, scenario, 1.0 / 60.0, r => runA = r);
    yield return RunScenario(runtimeAssets, scenario, 1.0 / 60.0, r => runB = r);

    if (!IsSuccess(runA) || !IsSuccess(runB)) {
      AppendLine("<color=red>FAIL</color> repeatability | scenario execution failed", showOnScreen: false);
      AppendScreenLine("重复运行校验: 失败");
      yield break;
    }

    AppendLine($"[RUN] repeatability-A checksum={runA.ChecksumDigest} state={runA.StateDigest} finalChecksum={runA.FinalChecksum} finalState={runA.FinalState}", showOnScreen: false);
    AppendLine($"[RUN] repeatability-B checksum={runB.ChecksumDigest} state={runB.StateDigest} finalChecksum={runB.FinalChecksum} finalState={runB.FinalState}", showOnScreen: false);

    var checksumStable = runA.ChecksumDigest == runB.ChecksumDigest;
    var stateStable = runA.StateDigest == runB.StateDigest;
    var finalChecksumStable = runA.FinalChecksum == runB.FinalChecksum;
    var finalStateStable = runA.FinalState == runB.FinalState;

    if (checksumStable && stateStable && finalChecksumStable && finalStateStable) {
      AppendLine($"<color=lime>PASS</color> repeatability | checksumDigest={runA.ChecksumDigest} stateDigest={runA.StateDigest}", showOnScreen: false);
      AppendScreenLine($"重复运行校验: 通过  哈希={ShortHash(runA.ChecksumDigest)}");
    } else {
      AppendLine($"<color=red>FAIL</color> repeatability | checksumStable={checksumStable} stateStable={stateStable} finalChecksumStable={finalChecksumStable} finalStateStable={finalStateStable} A={runA.ChecksumDigest}/{runA.StateDigest}/{runA.FinalChecksum} B={runB.ChecksumDigest}/{runB.StateDigest}/{runB.FinalChecksum}", showOnScreen: false);
      AppendScreenLine("重复运行校验: 失败");
    }
  }

  private IEnumerator RunCadenceCompare(RuntimeAssets runtimeAssets) {
    var scenario = FindScenario("dual-opposed-2p");
    AppendLine("[RUN] service-cadence-compare", showOnScreen: false);

    ScenarioResult at60 = null;
    ScenarioResult at120 = null;
    ScenarioResult at30 = null;

    yield return RunScenario(runtimeAssets, scenario, 1.0 / 60.0, r => at60 = r);
    yield return RunScenario(runtimeAssets, scenario, 1.0 / 120.0, r => at120 = r);
    yield return RunScenario(runtimeAssets, scenario, 1.0 / 30.0, r => at30 = r);

    if (!IsSuccess(at60) || !IsSuccess(at120) || !IsSuccess(at30)) {
      AppendLine("<color=red>FAIL</color> cadence | one or more runs failed", showOnScreen: false);
      AppendScreenLine("不同刷新频率校验: 失败");
      yield break;
    }

    AppendLine($"[RUN] cadence-60 checksum={at60.ChecksumDigest} state={at60.StateDigest} finalChecksum={at60.FinalChecksum} finalState={at60.FinalState}", showOnScreen: false);
    AppendLine($"[RUN] cadence-120 checksum={at120.ChecksumDigest} state={at120.StateDigest} finalChecksum={at120.FinalChecksum} finalState={at120.FinalState}", showOnScreen: false);
    AppendLine($"[RUN] cadence-30 checksum={at30.ChecksumDigest} state={at30.StateDigest} finalChecksum={at30.FinalChecksum} finalState={at30.FinalState}", showOnScreen: false);

    var checksumStable = at60.ChecksumDigest == at120.ChecksumDigest &&
                         at60.ChecksumDigest == at30.ChecksumDigest;
    var finalChecksumStable = at60.FinalChecksum == at120.FinalChecksum &&
                              at60.FinalChecksum == at30.FinalChecksum;
    var finalStateStable = at60.FinalState == at120.FinalState &&
                           at60.FinalState == at30.FinalState;
    var stateDigestStable = at60.StateDigest == at120.StateDigest &&
                            at60.StateDigest == at30.StateDigest;
    var passed = checksumStable && finalChecksumStable && finalStateStable;

    if (passed) {
      AppendLine($"<color=lime>PASS</color> cadence | checksumDigest={at60.ChecksumDigest} finalChecksum={at60.FinalChecksum} stateDigestStable={stateDigestStable}", showOnScreen: false);
      if (!stateDigestStable) {
        AppendLine("[WARN] cadence stateDigest differs because checkpoint sampling depends on service cadence; checksum/final state stayed stable.", showOnScreen: false);
      }

      AppendScreenLine($"不同刷新频率校验: 通过");
      AppendScreenLine($"刷新频率哈希: {ShortHash(at60.ChecksumDigest)}");
      AppendScreenLine($"刷新频率最终值: {at60.FinalChecksum}");
    } else {
      AppendLine($"<color=red>FAIL</color> cadence | 60={at60.ChecksumDigest}/{at60.StateDigest}/{at60.FinalChecksum} 120={at120.ChecksumDigest}/{at120.StateDigest}/{at120.FinalChecksum} 30={at30.ChecksumDigest}/{at30.StateDigest}/{at30.FinalChecksum}", showOnScreen: false);
      AppendScreenLine("不同刷新频率校验: 失败");
    }
  }

  private IEnumerator RunScenario(RuntimeAssets runtimeAssets, ScenarioDefinition scenario, double serviceStep, Action<ScenarioResult> onComplete) {
    SessionRunner runner = null;
    DynamicAssetDB dynamicDb = null;
    var callbackDispatcher = new CallbackDispatcher();
    var eventDispatcher = new EventDispatcher();
    var checksums = new List<ChecksumPoint>(256);
    var stateCheckpoints = new List<string>(32);
    var stateDigestIndex = 0;
    var playersAdded = false;
    var lastObservedFrame = -1;
    var targetFinalFrame = scenario.TickCount + FinalFrameOffset;
    ScenarioResult snapshotResult = null;
    ScenarioResult result = null;
    var scenarioFailed = false;
    AppendLine($"[SCENARIO] {scenario.Name} players={scenario.PlayerCount} seed={scenario.Seed} ticks={scenario.TickCount} targetFinalFrame={targetFinalFrame}", showOnScreen: false);

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
      AppendLine($"[SCENARIO] {scenario.Name} game started resync={callback.IsResync} players={scenario.PlayerCount}", showOnScreen: false);
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
      ZLog.LogException(ex);
      result = new ScenarioResult {
        Error = ex.ToString()
      };
      scenarioFailed = true;
    }

    if (scenarioFailed) {
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
        _summary = $"启动 {scenario.Name}...";
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

    while (runner.Session?.FramePredicted == null || runner.Session.FramePredicted.Number < targetFinalFrame) {
      try {
        runner.Service(serviceStep);
        QuantumUnityDB.UpdateGlobal();
      } catch (Exception ex) {
        ZLog.LogException(ex);
        result = new ScenarioResult {
          Error = ex.ToString()
        };
        scenarioFailed = true;
      }

      if (scenarioFailed) {
        break;
      }

      var predicted = runner.Session?.FramePredicted as Frame;
      if (predicted != null && predicted.Number != lastObservedFrame) {
        lastObservedFrame = predicted.Number;
        while (stateDigestIndex < StateDigestFrames.Length && predicted.Number >= StateDigestFrames[stateDigestIndex]) {
          var targetFrame = StateDigestFrames[stateDigestIndex];
          var checkpoint = $"{targetFrame}@{predicted.Number}:{BuildStateSummary(ReadState(predicted))}";
          stateCheckpoints.Add(checkpoint);
          AppendLine($"[STATE] {scenario.Name} {checkpoint}", showOnScreen: false);
          stateDigestIndex++;
        }

        if (snapshotResult == null && predicted.Number >= targetFinalFrame) {
          var sampledState = ReadState(predicted);
          snapshotResult = new ScenarioResult {
            FinalFrame = predicted.Number,
            FinalChecksum = FindChecksumForFrame(checksums, predicted.Number),
            ChecksumDigest = ComputeDigest(checksums),
            StateDigest = ComputeDigest(stateCheckpoints),
            FinalState = BuildStateSummary(sampledState)
          };
          AppendLine($"[SCENARIO] {scenario.Name} snapshot frame={snapshotResult.FinalFrame} finalChecksum={snapshotResult.FinalChecksum}", showOnScreen: false);
        }
      }

      if (predicted != null && predicted.Number % ServiceSlice == 0) {
        _summary = $"运行 {scenario.Name} 帧={predicted.Number}/{targetFinalFrame}";
        yield return null;
      }
    }

    if (scenarioFailed) {
      CompleteScenario(runner, dynamicDb, onComplete, result);
      yield return null;
      yield break;
    }

    if (snapshotResult == null) {
      var finalFrame = runner.Session.FramePredicted as Frame;
      if (finalFrame == null) {
        result = new ScenarioResult {
          Error = $"Predicted frame is not a Quantum.Frame for {scenario.Name}"
        };
        CompleteScenario(runner, dynamicDb, onComplete, result);
        yield return null;
        yield break;
      }

      var finalState = ReadState(finalFrame);
      snapshotResult = new ScenarioResult {
        FinalFrame = finalFrame.Number,
        FinalChecksum = FindChecksumForFrame(checksums, finalFrame.Number),
        ChecksumDigest = ComputeDigest(checksums),
        StateDigest = ComputeDigest(stateCheckpoints),
        FinalState = BuildStateSummary(finalState)
      };
      AppendLine($"[WARN] {scenario.Name} fallback snapshot frame={snapshotResult.FinalFrame} finalChecksum={snapshotResult.FinalChecksum}", showOnScreen: false);
    }

    if (string.IsNullOrEmpty(snapshotResult.FinalChecksum)) {
      result = new ScenarioResult {
        Error = $"Missing checksum for sampled frame in {scenario.Name}"
      };
      CompleteScenario(runner, dynamicDb, onComplete, result);
      yield return null;
      yield break;
    }

    result = snapshotResult;

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
        if (entry.Path != null && entry.Path.Contains("DefaultSystemsConfig")) {
          systemsEntry = entry;
        } else if (entry.Path != null && entry.Path.Contains("DefaultConfigSystems")) {
          systemsEntry ??= entry;
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

  private void EnsureRequiredQuantumGlobals() {
    AppendLine("[BOOT] Binding Quantum global assets", showOnScreen: false);
    EnsureGlobal<QuantumLookupTables>("QuantumLookupTables");
    EnsureGlobal<QuantumDefaultConfigs>("QuantumDefaultConfigs");
    EnsureGlobal<QuantumDeterministicSessionConfigAsset>("SessionConfig");
    EnsureGlobal<PhotonServerSettings>("PhotonServerSettings");
    EnsureGlobal<QuantumMeshCollection>("QuantumMeshCollection");
    EnsureQuantumUnityDb();
  }

  private void EnsureGlobal<T>(string resourcePath) where T : QuantumGlobalScriptableObject<T> {
    if (QuantumGlobalScriptableObject<T>.TryGetGlobal(out var existing) && existing != null) {
      AppendLine($"[BOOT] Global ready {typeof(T).Name} iid={existing.GetInstanceID()}", showOnScreen: false);
      return;
    }

    var asset = Resources.Load<T>(resourcePath);
    if (asset == null) {
      var all = Resources.LoadAll<T>(string.Empty);
      if (all != null && all.Length > 0) {
        var expectedName = Path.GetFileNameWithoutExtension(resourcePath);
        for (var i = 0; i < all.Length; i++) {
          if (string.Equals(all[i].name, expectedName, StringComparison.OrdinalIgnoreCase)) {
            asset = all[i];
            break;
          }
        }

        asset ??= all[0];
        AppendLine($"[BOOT] Fallback load {typeof(T).Name} count={all.Length} selected={asset.name}", showOnScreen: false);
      }
    }

    if (asset == null) {
      AppendLine($"<color=red>BOOT FAIL</color> Missing global asset {typeof(T).Name} resourcePath={resourcePath}", showOnScreen: false);
      return;
    }

    var property = typeof(T).GetProperty("Global", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
    if (property == null) {
      AppendLine($"<color=red>BOOT FAIL</color> Global property not found for {typeof(T).Name}", showOnScreen: false);
      return;
    }

    try {
      property.SetValue(null, asset);
      AppendLine($"[BOOT] Bound {typeof(T).Name} asset={asset.name}", showOnScreen: false);
    } catch (Exception ex) {
      ZLog.LogException(ex);
      AppendLine($"<color=red>BOOT FAIL</color> SetGlobal {typeof(T).Name} failed {ex.Message}", showOnScreen: false);
    }
  }

  private void EnsureQuantumUnityDb() {
    if (QuantumUnityDB.TryGetGlobal(out var existing) && existing != null) {
      AppendLine($"[BOOT] Global ready {nameof(QuantumUnityDB)} iid={existing.GetInstanceID()} entries={existing.Entries.Count}", showOnScreen: false);
      return;
    }

    AppendLine("[BOOT] Building runtime QuantumUnityDB", showOnScreen: false);
    var db = ScriptableObject.CreateInstance<QuantumUnityDB>();
    var added = 0;

    void TryAdd(AssetObject asset, string reason) {
      if (asset == null) {
        return;
      }

      try {
        db.AddAsset(asset);
        added++;
        AppendLine($"[BOOT] DB add {asset.GetType().Name} name={asset.name} path={asset.Path} reason={reason}", showOnScreen: false);
      } catch (Exception ex) {
        ZLog.LogException(ex);
        AppendLine($"<color=red>BOOT FAIL</color> DB add failed asset={asset.name} reason={reason} error={ex.Message}", showOnScreen: false);
      }
    }

    if (QuantumDefaultConfigs.TryGetGlobal(out var defaults) && defaults != null) {
      TryAdd(defaults.SimulationConfig, "default-config");
      TryAdd(defaults.SystemsConfig, "default-config");
      TryAdd(defaults.PhysicsMaterial, "default-config");
      TryAdd(defaults.CharacterController2DConfig, "default-config");
      TryAdd(defaults.CharacterController3DConfig, "default-config");
      TryAdd(defaults.NavMeshAgentConfig, "default-config");
    }

    var resourceAssets = Resources.LoadAll<AssetObject>(string.Empty);
    AppendLine($"[BOOT] Resource AssetObject count={resourceAssets.Length}", showOnScreen: false);
    for (var i = 0; i < resourceAssets.Length; i++) {
      TryAdd(resourceAssets[i], "resources");
    }

    QuantumUnityDB.Global = db;
    AppendLine($"[BOOT] Built {nameof(QuantumUnityDB)} entries={db.Entries.Count} added={added}", showOnScreen: false);
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
      AppendLine($"<color=red>FAIL</color> {name} | {result?.Error ?? "null result"}", showOnScreen: false);
      AppendScreenLine($"{ToChineseName(name)}: 失败");
      AppendScreenLine($"原因: {ShortenForScreen(result?.Error ?? "null result")}");
      return;
    }

    AppendLine($"<color=lime>OK</color> {name}", showOnScreen: false);
    AppendLine($"  checksumDigest={result.ChecksumDigest}", showOnScreen: false);
    AppendLine($"  stateDigest={result.StateDigest}", showOnScreen: false);
    AppendLine($"  finalChecksum={result.FinalChecksum} finalFrame={result.FinalFrame}", showOnScreen: false);
    AppendLine($"  finalState={result.FinalState}", showOnScreen: false);

    AppendScreenLine($"{ToChineseName(name)}: 通过");
    AppendScreenLine($"校验哈希: {ShortHash(result.ChecksumDigest)}");
    AppendScreenLine($"状态哈希: {ShortHash(result.StateDigest)}");
    AppendScreenLine($"最终值: {result.FinalChecksum}");
  }

  private static string BuildStateSummary(FrameSyncKernelState state) {
    return $"tick={state.Tick} p0={state.P0Position} v0={state.P0Velocity} e0={state.P0Energy} p1={state.P1Position} v1={state.P1Velocity} e1={state.P1Energy} dist={state.LastDistance} acc={state.Accumulator} bounce={state.BounceCount} action={state.ActionCount}";
  }

  private static FrameSyncKernelState ReadState(Frame frame) {
    return *frame.Unsafe.GetPointerSingleton<FrameSyncKernelState>();
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

  private static string FindChecksumForFrame(List<ChecksumPoint> values, int frame) {
    for (var i = values.Count - 1; i >= 0; i--) {
      if (values[i].Frame == frame) {
        return values[i].Checksum;
      }
    }

    return string.Empty;
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

  private void AppendLine(string line, bool showOnScreen = true) {
    var formatted = $"[{DateTime.Now:HH:mm:ss}] {line}";
    _lines.Add(formatted);
    if (_lines.Count > 240) {
      _lines.RemoveAt(0);
    }

    if (showOnScreen) {
      _screenLines.Add(formatted);
      if (_screenLines.Count > 40) {
        _screenLines.RemoveAt(0);
      }
    }

    var plain = RichTextRegex.Replace(formatted, string.Empty);
    if (plain.IndexOf("FAIL", StringComparison.OrdinalIgnoreCase) >= 0 ||
        plain.IndexOf("BOOTSTRAP FAIL", StringComparison.OrdinalIgnoreCase) >= 0) {
      ZLog.LogError(plain);
    } else if (plain.IndexOf("WARN", StringComparison.OrdinalIgnoreCase) >= 0) {
      ZLog.LogWarning(plain);
    } else {
      ZLog.LogMobile(plain);
    }
  }

  private void AppendScreenLine(string line) {
    _screenLines.Add(line);
    if (_screenLines.Count > ScreenLineLimit) {
      _screenLines.RemoveAt(0);
    }
  }

  private static string ShortHash(string value) {
    if (string.IsNullOrEmpty(value)) {
      return "null";
    }

    if (value.Length <= ScreenHashLength * 2) {
      return value;
    }

    return $"{value.Substring(0, ScreenHashLength)}...{value.Substring(value.Length - ScreenHashLength, ScreenHashLength)}";
  }

  private static string ShortenForScreen(string value) {
    if (string.IsNullOrEmpty(value)) {
      return "unknown error";
    }

    value = value.Replace(Environment.NewLine, " ");
    return value.Length <= 72 ? value : $"{value.Substring(0, 72)}...";
  }

  private static string ToChineseName(string name) {
    return name switch {
      "idle-1p" => "单人静止",
      "move-1p" => "单人直线移动",
      "weave-1p" => "单人折返移动",
      "dual-opposed-2p" => "双人对抗",
      _ => name
    };
  }

  private void FinishRun() {
    _summary = "完成";
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
