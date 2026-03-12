namespace Photon.Deterministic
{
	/// <summary>
	/// Delegate for event received notifications.
	/// </summary>
	/// <param name="eventCode">Photon message event code</param>
	/// <param name="data">Message data</param>
	/// <param name="dataLength">Message length</param>
	/// <param name="dataContainer">The container of the data to recycle objects, see <see cref="M:Photon.Deterministic.ICommunicator.DisposeEventObject(System.Object)" /></param>
	public delegate void OnEventReceived(byte eventCode, byte[] data, int dataLength, object dataContainer);
}

