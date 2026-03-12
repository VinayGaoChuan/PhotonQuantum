namespace Photon.Client;

/// <summary>Enum of the three options for reliability and sequencing in Photon's reliable-UDP.</summary>
public enum DeliveryMode
{
	/// <summary>The operation/message gets sent just once without acknowledgement or repeat. The sequence (order) of messages is guaranteed.</summary>
	Unreliable,
	/// <summary>The operation/message asks for an acknowledgment. It's resent until an ACK arrived. The sequence (order) of messages is guaranteed.</summary>
	Reliable,
	/// <summary>The operation/message gets sent once (unreliable) and might arrive out of order. Best for your own sequencing (e.g. for streams).</summary>
	UnreliableUnsequenced,
	/// <summary>The operation/message asks for an acknowledgment. It's resent until an ACK arrived and might arrive out of order. Best for your own sequencing (e.g. for streams).</summary>
	ReliableUnsequenced
}
