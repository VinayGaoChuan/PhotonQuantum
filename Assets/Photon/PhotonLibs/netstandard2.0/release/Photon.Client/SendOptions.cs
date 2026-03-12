namespace Photon.Client;

/// <summary>Wraps up DeliveryMode, Encryption and Channel values for sending operations and messages.</summary>
public struct SendOptions
{
	/// <summary>Default SendOptions instance for reliable sending.</summary>
	public static readonly SendOptions SendReliable = new SendOptions
	{
		Reliability = true
	};

	/// <summary>Default SendOptions instance for unreliable sending.</summary>
	public static readonly SendOptions SendUnreliable = new SendOptions
	{
		Reliability = false
	};

	/// <summary>Chose the DeliveryMode for this operation/message. Defaults to Unreliable.</summary>
	public DeliveryMode DeliveryMode;

	/// <summary>If true the operation/message gets encrypted before it's sent. Defaults to false.</summary>
	/// <remarks>Before encryption can be used, it must be established. Check PhotonPeer.IsEncryptionAvailable is true.</remarks>
	public bool Encrypt;

	/// <summary>The Enet channel to send in. Defaults to 0.</summary>
	/// <remarks>Channels in Photon relate to "message channels". Each channel is a sequence of messages.</remarks>
	public byte Channel;

	/// <summary>Sets the DeliveryMode either to true: Reliable or false: Unreliable, overriding any current value.</summary>
	/// <remarks>Use this to conveniently select reliable/unreliable delivery.</remarks>
	public bool Reliability
	{
		get
		{
			return DeliveryMode == DeliveryMode.Reliable;
		}
		set
		{
			DeliveryMode = (value ? DeliveryMode.Reliable : DeliveryMode.Unreliable);
		}
	}
}
