using System;

namespace Photon.Client;

[Flags]
internal enum InitV3Flags : short
{
	NoFlags = 0,
	EncryptionFlag = 1,
	IPv6Flag = 2,
	ReleaseSdkFlag = 4
}
