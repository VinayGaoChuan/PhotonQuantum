using System;
using System.Collections.Generic;
using UnityEngine;
using Quantum;

namespace Quantum {
  //todo by huayu  不支持小程序 webgl
  public class SimpleEntityViewUpdater : QuantumMonoBehaviour {
    [Header("Entity View Settings")] 
    public Transform ViewParentTransform;
    
    public QuantumSnapshotInterpolationTimer SnapshotInterpolation = new QuantumSnapshotInterpolationTimer();

    class ViewCreationRequest {
      public EntityRef EntityRef;
      public EntityView EntityView;
      public Vector3 Position;
      public Quaternion Rotation;
      public bool HasTransform;
    }

    private QuantumGame _observedGame;
    private Dictionary<Type, IQuantumViewContext> _viewContexts;

    private readonly Dictionary<EntityRef, QuantumEntityView> _activeViews =
      new Dictionary<EntityRef, QuantumEntityView>();

    private readonly HashSet<EntityRef> _activeEntities = new HashSet<EntityRef>();
    private readonly HashSet<EntityRef> _removeEntities = new HashSet<EntityRef>();
    
    private readonly Dictionary<AssetGuid, Queue<QuantumEntityView>> _viewPool = 
      new Dictionary<AssetGuid, Queue<QuantumEntityView>>();

    private readonly Dictionary<int, Dictionary<AssetGuid, List<ViewCreationRequest>>> _batchRequests = 
      new Dictionary<int, Dictionary<AssetGuid, List<ViewCreationRequest>>>();
    
    private readonly HashSet<EntityRef> _pendingInstantiation = new HashSet<EntityRef>();
    
    private int _currentFrameIndex = -1;
    private bool _isFrameEnd = false;
    private const int k_FrameBufferWindow = 8;

    public QuantumGame ObservedGame => _observedGame; 
    public Dictionary<Type, IQuantumViewContext> Context => _viewContexts;

    public QuantumEntityView GetView(EntityRef entityRef) {
      _activeViews.TryGetValue(entityRef, out QuantumEntityView view);
      return view;
    }
    
    public void SetCurrentGame(QuantumGame game) {
      _observedGame = game;
    }

    private void Awake() {
      LoadViewContexts();
      
      QuantumCallback.Subscribe(this, (CallbackGameInit c) => OnGameInit(c.Game));
      QuantumCallback.Subscribe(this, (CallbackUpdateView c) => OnGameUpdated(c.Game), game => game == _observedGame);
      QuantumCallback.Subscribe(this, (CallbackGameDestroyed c) => OnGameDestroyed(c.Game),
        game => game == _observedGame);
    }
    
    private void LoadViewContexts() {
      if (_viewContexts != null) {
        return;
      }

      _viewContexts = new Dictionary<Type, IQuantumViewContext>();
      var contexts = GetComponentsInChildren<IQuantumViewContext>();
      foreach (var c in contexts) {
        if (_viewContexts.ContainsKey(c.GetType())) {
          Debug.LogError($"The view context type {c.GetType()} already exists. Multiple contexts of the same type are not supported.");
        } else {
          _viewContexts.Add(c.GetType(), c);
        }
      }
    }

    private void OnGameInit(QuantumGame game) {
      if (_observedGame == null) {
        _observedGame = game;
      }
    }

    private void OnGameDestroyed(QuantumGame game) {
      foreach (var view in _activeViews) {
        if (view.Value) {
          DestroyView(view.Value);
        }
      }

      _activeViews.Clear();
      ClearPool();
      ClearBatchRequests();
      _pendingInstantiation.Clear();
      _observedGame = null;
    }

    private void OnGameUpdated(QuantumGame game) {
      if (!isActiveAndEnabled || game.Frames.Predicted == null) {
        return;
      }

      _currentFrameIndex++;
      _isFrameEnd = false;

      var verifiedFrame = game.Frames.Verified;
      if (verifiedFrame != null) {
        SnapshotInterpolation.Advance(verifiedFrame.Number, 1f / game.Session.SessionConfig.UpdateFPS);
      }

      _activeEntities.Clear();

      SyncViews(game, game.Frames.Predicted);

      _removeEntities.Clear();
      foreach (var kvp in _activeViews) {
        if (!_activeEntities.Contains(kvp.Key) && !_pendingInstantiation.Contains(kvp.Key)) {
          _removeEntities.Add(kvp.Key);
        }
      }

      foreach (var entityRef in _removeEntities) {
        DestroyView(entityRef);
      }

      ProcessBatchCreation(game, game.Frames.Predicted);

      foreach (var kvp in _activeViews) {
        if (kvp.Value) {
          kvp.Value.UpdateView(true, false);
        }
      }
      
      _isFrameEnd = true;
    }

    private void LateUpdate() {
      if (_observedGame != null) {
        foreach (var kvp in _activeViews) {
          kvp.Value.LateUpdateView();
        }
      }
    }

    private void SyncViews(QuantumGame game, Frame frame) {
      foreach (var (entity, view) in frame.GetComponentIterator<View>()) {
        CreateViewIfNeeded(game, frame, entity, view);
      }
    }

    private void CreateViewIfNeeded(QuantumGame game, Frame frame, EntityRef entityRef, View view) {
      var entityView = frame.FindAsset<EntityView>(view.Current.Id);

      if (_activeViews.TryGetValue(entityRef, out var instance)) {
        if (entityView == null) {
          DestroyView(entityRef);
        } else if (instance.AssetGuid == entityView.Guid) {
          _activeEntities.Add(entityRef);
        } else {
          DestroyView(entityRef);
          EnqueueViewCreation(frame, entityRef, entityView);
          _activeEntities.Add(entityRef);
        }
      } else if (entityView != null) {
        if (!_pendingInstantiation.Contains(entityRef)) {
          EnqueueViewCreation(frame, entityRef, entityView);
          _activeEntities.Add(entityRef);
        }
      }
    }

    private unsafe void EnqueueViewCreation(Frame frame, EntityRef entityRef, EntityView entityView) {
      if (entityView?.Prefab == null) {
        return;
      }

      var viewComponent = entityView.Prefab.GetComponent<QuantumEntityView>();
      if (viewComponent == null) {
        return;
      }

      var pooledView = GetFromPool(entityView.Guid);
      if (pooledView != null) {
        ActivatePooledView(frame, entityRef, entityView, pooledView);
        return;
      }

      int frameIndex = _isFrameEnd ? _currentFrameIndex + 1 : _currentFrameIndex;
      
      if (!_batchRequests.TryGetValue(frameIndex, out var assetDict)) {
        assetDict = new Dictionary<AssetGuid, List<ViewCreationRequest>>();
        _batchRequests[frameIndex] = assetDict;
      }

      if (!assetDict.TryGetValue(entityView.Guid, out var requestList)) {
        requestList = new List<ViewCreationRequest>();
        assetDict[entityView.Guid] = requestList;
      }

      TryGetTransform(frame, entityRef, out Vector3 position, out Quaternion rotation);
      
      requestList.Add(new ViewCreationRequest {
        EntityRef = entityRef,
        EntityView = entityView,
        Position = position,
        Rotation = rotation,
        HasTransform = frame.Unsafe.TryGetPointer(entityRef, out Transform3D* _) || 
                      frame.Unsafe.TryGetPointer(entityRef, out Transform2D* _)
      });

      _pendingInstantiation.Add(entityRef);

      CleanupOldFrames(frameIndex - k_FrameBufferWindow);
    }

    private void ActivatePooledView(Frame frame, EntityRef entityRef, EntityView entityView, QuantumEntityView view) {
      if (TryGetTransform(frame, entityRef, out Vector3 position, out Quaternion rotation)) {
        view.transform.SetPositionAndRotation(position, rotation);
      }

      view.EntityRef = entityRef;

      _activeViews[entityRef] = view;

      view.Activate(_observedGame, frame, Context, this);
      view.OnEntityInstantiated.Invoke(_observedGame);
    }

    private void ProcessBatchCreation(QuantumGame game, Frame frame) {
      if (!_batchRequests.TryGetValue(_currentFrameIndex, out var assetDict)) {
        return;
      }

      foreach (var kvp in assetDict) {
        var assetGuid = kvp.Key;
        var requests = kvp.Value;

        if (requests.Count == 0) continue;

        var snapshot = requests.ToArray();
        CreateViewBatch(game, frame, assetGuid, snapshot);
      }

      assetDict.Clear();
      _batchRequests.Remove(_currentFrameIndex);
    }

    private void CreateViewBatch(QuantumGame game, Frame frame, AssetGuid assetGuid, ViewCreationRequest[] requests) {
      if (requests == null || requests.Length == 0) return;

      var firstRequest = requests[0];
      var viewComponent = firstRequest.EntityView.Prefab.GetComponent<QuantumEntityView>();
      if (viewComponent == null) {
        foreach (var req in requests) {
          _pendingInstantiation.Remove(req.EntityRef);
        }
        return;
      }

      GameObject[] instances = null;
      try {
        instances =  InstantiateAsyncBatch(viewComponent.gameObject, requests.Length);
      } catch (Exception ex) {
        Debug.LogError($"[SimpleEntityViewUpdater] Batch instantiation failed: {ex}");
        foreach (var req in requests) {
          _pendingInstantiation.Remove(req.EntityRef);
        }
        return;
      }

      for (int i = 0; i < instances.Length; i++) {
        var go = instances[i];
        var req = requests[i];

        if (_removeEntities.Contains(req.EntityRef)) {
          if (go != null) Destroy(go);
          _pendingInstantiation.Remove(req.EntityRef);
          continue;
        }

        if (go == null) {
          _pendingInstantiation.Remove(req.EntityRef);
          continue;
        }

        var instance = go.GetComponent<QuantumEntityView>();
        if (instance == null) {
          Destroy(go);
          _pendingInstantiation.Remove(req.EntityRef);
          continue;
        }

        if (req.HasTransform) {
          instance.transform.SetPositionAndRotation(req.Position, req.Rotation);
        }

        if (ViewParentTransform != null) {
          instance.transform.SetParent(ViewParentTransform,true);
        }

        instance.AssetGuid = assetGuid;
        //instance.gameObject.name = req.EntityRef.ToString();
        instance.EntityRef = req.EntityRef;

        if (_activeViews.ContainsKey(req.EntityRef)) {
          var oldView = _activeViews[req.EntityRef];
          if (oldView != null) {
            DestroyView(oldView);
          }
        }

        _activeViews[req.EntityRef] = instance;

        instance.Activate(game, frame, Context, this);
        instance.OnEntityInstantiated.Invoke(game);

        _pendingInstantiation.Remove(req.EntityRef);
      }
    }

    private GameObject[] InstantiateAsyncBatch(GameObject prefab, int count) {
      var operation = InstantiateAsync<GameObject>(prefab, count);
      operation.WaitForCompletion();
      return operation.Result;
    }

    private void CleanupOldFrames(int minKeepFrame) {
      if (_batchRequests.Count == 0) return;

      var toRemove = new List<int>();
      foreach (var key in _batchRequests.Keys) {
        if (key < minKeepFrame) {
          toRemove.Add(key);
        }
      }

      foreach (var frameKey in toRemove) {
        if (_batchRequests.TryGetValue(frameKey, out var assetDict)) {
          foreach (var requests in assetDict.Values) {
            foreach (var req in requests) {
              _pendingInstantiation.Remove(req.EntityRef);
            }
            requests.Clear();
          }
          assetDict.Clear();
        }
        _batchRequests.Remove(frameKey);
      }
    }

    private void DestroyView(EntityRef entityRef) {
      if (_activeViews.TryGetValue(entityRef, out var view)) {
        DestroyView(view);
        _activeViews.Remove(entityRef);
      }

      _pendingInstantiation.Remove(entityRef);

      int maxProbe = _currentFrameIndex + k_FrameBufferWindow;
      for (int frame = _currentFrameIndex; frame <= maxProbe; frame++) {
        if (!_batchRequests.TryGetValue(frame, out var assetDict)) continue;

        foreach (var requests in assetDict.Values) {
          for (int i = requests.Count - 1; i >= 0; i--) {
            if (requests[i].EntityRef == entityRef) {
              requests.RemoveAt(i);
            }
          }
        }
      }
    }

    private void DestroyView(QuantumEntityView view) {
      if (view == null) {
        return;
      }

      view.OnEntityDestroyed.Invoke(_observedGame);

      if (!view.ManualDisposal) {
        view.Deactivate();
        ReturnToPool(view);
      }
    }

    private QuantumEntityView GetFromPool(AssetGuid assetGuid) {
      if (!_viewPool.TryGetValue(assetGuid, out var queue)) {
        return null;
      }

      while (queue.Count > 0) {
        var view = queue.Dequeue();
        if (view != null && view.gameObject != null) {
          view.gameObject.SetActive(true);
          return view;
        }
      }

      return null;
    }

    private void ReturnToPool(QuantumEntityView view) {
      if (view == null || view.gameObject == null) {
        return;
      }

      var assetGuid = view.AssetGuid;
      if (!_viewPool.TryGetValue(assetGuid, out var queue)) {
        queue = new Queue<QuantumEntityView>();
        _viewPool[assetGuid] = queue;
      }

      view.gameObject.SetActive(false);
      view.transform.SetParent(ViewParentTransform);
      queue.Enqueue(view);
    }

    private void ClearPool() {
      foreach (var queue in _viewPool.Values) {
        while (queue.Count > 0) {
          var view = queue.Dequeue();
          if (view != null && view.gameObject != null) {
            Destroy(view.gameObject);
          }
        }
      }
      _viewPool.Clear();
    }

    private void ClearBatchRequests() {
      foreach (var assetDict in _batchRequests.Values) {
        foreach (var requests in assetDict.Values) {
          requests.Clear();
        }
        assetDict.Clear();
      }
      _batchRequests.Clear();
    }

    private unsafe bool TryGetTransform(Frame frame, EntityRef entityRef, out Vector3 position,
      out Quaternion rotation) {
      if (frame.Unsafe.TryGetPointer(entityRef, out Transform3D* transform3D)) {
        position = transform3D->Position.ToUnityVector3();
        rotation = transform3D->Rotation.ToUnityQuaternion();
        return true;
      }

      if (frame.Unsafe.TryGetPointer(entityRef, out Transform2D* transform2D)) {
        position = transform2D->Position.ToUnityVector3();
        rotation = transform2D->Rotation.ToUnityQuaternion();
        return true;
      }

      position = Vector3.zero;
      rotation = Quaternion.identity;
      return false;
    }

    private void OnDestroy() {
      foreach (var kvp in _activeViews) {
        if (kvp.Value && kvp.Value.gameObject) {
          Destroy(kvp.Value.gameObject);
        }
      }
      
      ClearPool();
      ClearBatchRequests();
      _pendingInstantiation.Clear();
    }
  }
}