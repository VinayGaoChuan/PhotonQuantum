namespace Photon.Client;

internal enum EgMessageType : byte
{
	Init,
	InitResponse,
	Operation,
	OperationResponse,
	Event,
	DisconnectReason,
	InternalOperationRequest,
	InternalOperationResponse,
	Message,
	RawMessage
}
