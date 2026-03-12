namespace Photon.Deterministic
{
	/// <summary>
	/// Definition of Photon room properties with additional functionality.
	/// </summary>
	public static class RoomProperties
	{
		/// <summary>
		/// This room property can be controlled through Photon dashboard settings.
		/// </summary>
		public static string Start = "StartQuantum";

		/// <summary>
		/// User can set this during CreateGame to change the webhook base url provided by an allow list.
		/// </summary>
		public static string WebHookBaseUrl = "QuantumWebHookBaseUrl";
	}
}

