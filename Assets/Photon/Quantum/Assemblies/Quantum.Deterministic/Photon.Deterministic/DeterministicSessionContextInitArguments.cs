namespace Photon.Deterministic
{
	/// <summary>
	/// The arguments to initialize a <see cref="T:Photon.Deterministic.IDeterministicSessionContext" />.
	/// </summary>
	public struct DeterministicSessionContextInitArguments
	{
		/// <summary>
		/// The path to folder that contains the look up tables.
		/// </summary>
		public string LutPath;

		/// <summary>
		/// The path to the asset database to load.
		/// </summary>
		public string AssetDBPath;

		/// <summary>
		/// The name of the embedded asset database to load.
		/// </summary>
		public string EmbeddedAssetDBName;

		/// <summary>
		/// The asset serializer object (IAssetSerializer).
		/// </summary>
		public object AssetSerializer;
	}
}

