using System;
using System.Numerics;
using System.Security.Cryptography;

namespace Photon.Client.Encryption;

internal class DiffieHellmanCryptoProvider : ICryptoProvider, IDisposable
{
	private static readonly BigInteger primeRoot = new BigInteger(OakleyGroups.Generator);

	private readonly BigInteger prime;

	private readonly BigInteger secret;

	private readonly BigInteger publicKey;

	private Rijndael crypto;

	private byte[] sharedKey;

	public bool IsInitialized => crypto != null;

	/// <summary>
	/// Gets the public key that can be used by another DiffieHellmanCryptoProvider object
	/// to generate a shared secret agreement.
	/// </summary>
	public byte[] PublicKey
	{
		get
		{
			BigInteger bigInteger = publicKey;
			return MsBigIntArrayToPhotonBigIntArray(bigInteger.ToByteArray());
		}
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Photon.Client.Encryption.DiffieHellmanCryptoProvider" /> class.
	/// </summary>
	public DiffieHellmanCryptoProvider()
	{
		prime = new BigInteger(OakleyGroups.OakleyPrime768);
		secret = GenerateRandomSecret(160);
		publicKey = CalculatePublicKey();
	}

	public DiffieHellmanCryptoProvider(byte[] cryptoKey)
	{
		crypto = new RijndaelManaged();
		crypto.Key = cryptoKey;
		crypto.IV = new byte[16];
		crypto.Padding = PaddingMode.PKCS7;
	}

	/// <summary>
	/// Derives the shared key is generated from the secret agreement between two parties,
	/// given a byte array that contains the second party's public key.
	/// </summary>
	/// <param name="otherPartyPublicKey">
	/// The second party's public key.
	/// </param>
	public void DeriveSharedKey(byte[] otherPartyPublicKey)
	{
		otherPartyPublicKey = PhotonBigIntArrayToMsBigIntArray(otherPartyPublicKey);
		BigInteger key = new BigInteger(otherPartyPublicKey);
		sharedKey = MsBigIntArrayToPhotonBigIntArray(CalculateSharedKey(key).ToByteArray());
		byte[] hash;
		using (SHA256 hashProvider = new SHA256Managed())
		{
			hash = hashProvider.ComputeHash(sharedKey);
		}
		crypto = new RijndaelManaged();
		crypto.Key = hash;
		crypto.IV = new byte[16];
		crypto.Padding = PaddingMode.PKCS7;
	}

	private byte[] PhotonBigIntArrayToMsBigIntArray(byte[] array)
	{
		Array.Reverse((Array)array);
		if ((array[^1] & 0x80) == 128)
		{
			byte[] result = new byte[array.Length + 1];
			Buffer.BlockCopy(array, 0, result, 0, array.Length);
			return result;
		}
		return array;
	}

	private byte[] MsBigIntArrayToPhotonBigIntArray(byte[] array)
	{
		Array.Reverse((Array)array);
		if (array[0] == 0)
		{
			byte[] result = new byte[array.Length - 1];
			Buffer.BlockCopy(array, 1, result, 0, array.Length - 1);
			return result;
		}
		return array;
	}

	public byte[] Encrypt(byte[] data)
	{
		return Encrypt(data, 0, data.Length);
	}

	public byte[] Encrypt(byte[] data, int offset, int count)
	{
		using ICryptoTransform enc = crypto.CreateEncryptor();
		return enc.TransformFinalBlock(data, offset, count);
	}

	public byte[] Decrypt(byte[] data)
	{
		return Decrypt(data, 0, data.Length);
	}

	public byte[] Decrypt(byte[] data, int offset, int count)
	{
		using ICryptoTransform enc = crypto.CreateDecryptor();
		return enc.TransformFinalBlock(data, offset, count);
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	protected void Dispose(bool disposing)
	{
	}

	private BigInteger CalculatePublicKey()
	{
		return BigInteger.ModPow(primeRoot, secret, prime);
	}

	private BigInteger CalculateSharedKey(BigInteger otherPartyPublicKey)
	{
		return BigInteger.ModPow(otherPartyPublicKey, secret, prime);
	}

	private BigInteger GenerateRandomSecret(int secretLength)
	{
		RNGCryptoServiceProvider Crypto = new RNGCryptoServiceProvider();
		byte[] Buffer = new byte[secretLength / 8];
		BigInteger result;
		do
		{
			Crypto.GetBytes(Buffer);
			result = new BigInteger(Buffer);
		}
		while (result >= prime - 1 || result < 2L);
		return result;
	}
}
