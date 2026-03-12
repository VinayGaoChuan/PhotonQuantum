#if !QUANTUM_DEV

#region Assets/Photon/Quantum/Runtime/AssemblyAttributes/QuantumAssemblyAttributeMapBake.cs

// Tag this assembly to receive map bake callbacks, e.g. used to invalidate gizmo cache
[assembly: Quantum.QuantumMapBakeAssembly]


#endregion


#region Assets/Photon/Quantum/Runtime/AssemblyAttributes/QuantumAssemblyAttributes.Common.cs

// merged AssemblyAttributes

#region RegisterResourcesLoader.cs

// register a default loader; it will attempt to load the asset from their default paths if they happen to be Resources
[assembly: Quantum.QuantumGlobalScriptableObjectResource(typeof(Quantum.QuantumGlobalScriptableObject), Order = 2000, AllowFallback = true)]
[assembly: Quantum.QuantumGlobalScriptableObjectAddress(typeof(Quantum.QuantumUnityDB),"Assets/_Resources/QuantumConfigs/QuantumUnityDB.qunitydb", Order = 1000, AllowFallback = true)]
[assembly: Quantum.QuantumGlobalScriptableObjectAddress(typeof(Quantum.PhotonServerSettings),"Assets/_Resources/QuantumConfigs/PhotonServerSettings.asset", Order = 1000, AllowFallback = true)]
[assembly: Quantum.QuantumGlobalScriptableObjectAddress(typeof(Quantum.QuantumDefaultConfigs),"Assets/_Resources/QuantumConfigs/QuantumDefaultConfigs.asset", Order = 1000, AllowFallback = true)]
[assembly: Quantum.QuantumGlobalScriptableObjectAddress(typeof(Quantum.QuantumDeterministicSessionConfigAsset),"Assets/_Resources/QuantumConfigs/SessionConfig.asset", Order = 1000, AllowFallback = true)]
[assembly: Quantum.QuantumGlobalScriptableObjectAddress(typeof(Quantum.QuantumMeshCollection),"Assets/_Resources/QuantumConfigs/QuantumMeshCollection.asset", Order = 1000, AllowFallback = true)]
[assembly: Quantum.QuantumGlobalScriptableObjectAddress(typeof(Quantum.QuantumLookupTables),"Assets/_Resources/QuantumConfigs/QuantumLookupTables.asset", Order = 1000, AllowFallback = true)]
[assembly: Quantum.QuantumGlobalScriptableObjectAddress(typeof(Quantum.PhysicsMaterial),"Assets/_Resources/QuantumConfigs/DefaultPhysicsMaterial.asset", Order = 1000, AllowFallback = true)]



#endregion



#endregion

#endif
