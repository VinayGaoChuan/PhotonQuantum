namespace Photon.Client;

/// <summary>A Photon Event consists of a Code value and a Parameters Dictionary with the event's content (if any).</summary>
/// <remarks>
/// The indexer of this class provides access to the values in Parameters.
/// It wraps the null check for Parameters and uses TryGetValue() for the provided key.
///
/// Photon servers use events to send information which is not triggered by a client's operation requests (those get responses).
/// The Realtime API allows you to send custom events with any Code and content via OpRaiseEvent.
/// </remarks>
public class EventData
{
	/// <summary>The event code identifies the type of event.</summary>
	public byte Code;

	/// <summary>The Parameters of an event is a Dictionary&lt;byte, object&gt;.</summary>
	public readonly ParameterDictionary Parameters;

	/// <summary>
	/// (254) Defines the event key containing the Sender of the event.
	/// </summary>
	/// <remarks>
	/// Defaults to Sender key of Realtime API events (RaiseEvent): 254.
	/// Can be set to Chat API's ChatParameterCode.Sender: 5.
	/// </remarks>
	public byte SenderKey = 254;

	private int sender = -1;

	/// <summary>
	/// Defines the event key containing the Custom Data of the event.
	/// </summary>
	/// <remarks>
	/// Defaults to Data key of Realtime API events (RaiseEvent): 245.
	/// Can be set to any other value on demand.
	/// </remarks>
	public byte CustomDataKey = 245;

	private object customData;

	/// <summary>
	/// Access to the Parameters of a Photon-defined event. Custom Events only use Code, Sender and CustomData.
	/// </summary>
	/// <param name="key">The key byte-code of a Photon event value.</param>
	/// <returns>The Parameters value, or null if the key does not exist in Parameters.</returns>
	public object this[byte key]
	{
		get
		{
			Parameters.TryGetValue(key, out var o);
			return o;
		}
		internal set
		{
			Parameters.Add(key, value);
		}
	}

	/// <summary>
	/// Accesses the Sender of the event via the indexer and SenderKey. The result is cached.
	/// </summary>
	/// <remarks>
	/// Accesses this event's Parameters[CustomDataKey], which may be null.
	/// In that case, this returns 0 (identifying the server as sender).
	/// </remarks>
	public int Sender
	{
		get
		{
			if (sender == -1)
			{
				int val;
				bool found = Parameters.TryGetValue(SenderKey, out val);
				sender = (found ? val : (-1));
			}
			return sender;
		}
		internal set
		{
			sender = value;
		}
	}

	/// <summary>
	/// Accesses the Custom Data of the event via the indexer and CustomDataKey. The result is cached.
	/// </summary>
	/// <remarks>
	/// Accesses this event's Parameters[CustomDataKey], which may be null.
	/// </remarks>
	public object CustomData
	{
		get
		{
			if (customData == null)
			{
				Parameters.TryGetValue(CustomDataKey, out customData);
			}
			return customData;
		}
		internal set
		{
			customData = value;
		}
	}

	public EventData()
	{
		Parameters = new ParameterDictionary();
	}

	internal void Reset()
	{
		Code = 0;
		Parameters.Clear();
		sender = -1;
		customData = null;
	}

	/// <summary>ToString() override.</summary>
	/// <returns>Short output of "Event" and it's Code.</returns>
	public override string ToString()
	{
		return $"Event {Code.ToString()}.";
	}

	/// <summary>Extensive output of the event content.</summary>
	/// <returns>To be used in debug situations only, as it returns a string for each value.</returns>
	public string ToStringFull()
	{
		return $"Event {Code}: {SupportClass.DictionaryToString(Parameters)}";
	}
}
