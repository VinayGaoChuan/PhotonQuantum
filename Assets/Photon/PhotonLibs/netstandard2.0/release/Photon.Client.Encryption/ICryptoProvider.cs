using System;

namespace Photon.Client.Encryption;

/// <summary>Interface for Photon's DiffieHellman/Payload Encryption.</summary>
internal interface ICryptoProvider : IDisposable
{
	bool IsInitialized { get; }

	byte[] PublicKey { get; }

	void DeriveSharedKey(byte[] otherPartyPublicKey);

	byte[] Encrypt(byte[] data);

	byte[] Encrypt(byte[] data, int offset, int count);

	byte[] Decrypt(byte[] data);

	byte[] Decrypt(byte[] data, int offset, int count);
}
