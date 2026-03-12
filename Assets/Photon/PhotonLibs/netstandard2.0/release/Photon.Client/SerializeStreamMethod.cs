namespace Photon.Client;

/// <summary>Serialization method delegate. StreamBuffer based custom serialization methods must use this form.</summary>
/// <remarks>Use PhotonPeer.RegisterType() to register new types with serialization and deserialization methods.</remarks>
/// <param name="outStream">Write the bytes to reproduce the given customObject to this stream.</param>
/// <param name="customObject">Object to be serialized for sending over the network.</param>
/// <returns>Count of bytes written to serialize this object.</returns>
public delegate short SerializeStreamMethod(StreamBuffer outStream, object customObject);
