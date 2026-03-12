namespace Photon.Client;

/// <summary>Deserialization method delegate. StreamBuffer based custom deserialization methods must use this form.</summary>
/// <remarks>Use PhotonPeer.RegisterType() to register new types with serialization and deserialization methods.</remarks>
/// <param name="inStream">Incoming bytes for the custom type object. Wrapped in StreamBuffer.</param>
/// <param name="length">Bytes that belong to this object. These must be read by the deserialization method.</param>
/// <returns>Count the object as deserialized from the inStream.</returns>
public delegate object DeserializeStreamMethod(StreamBuffer inStream, short length);
