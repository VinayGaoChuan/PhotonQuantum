using System;
using Photon.Deterministic.Protocol;

namespace Photon.Deterministic
{
	/// <summary> 
	/// Parameterize internals of the Deterministic simulation and plugin (the Quantum server component). 
	/// </summary>
	/// <para>
	/// This config file will be synchronized between all clients of one session. Though each player starts its own simulation locally with his own version of the DeterministicConfig the server will distribute the config file instance of the first player that joined the plugin.
	/// </para>
	[Serializable]
	public class DeterministicSessionConfig
	{
		/// <summary> Player count the simulation is initialized for. </summary>
		public int PlayerCount;

		/// <summary> This allows Quantum frame checksumming to be deterministic across different runtime platforms, however it comes with quite a cost and should only be used during debugging. </summary>
		public bool ChecksumCrossPlatformDeterminism;

		/// <summary>
		/// Legacy feature that forces the Quantum simulation to run in strict lockstep mode, where no rollbacks are performed.
		/// Most games suitable for lockstep are better off using a soft lockstep instead by introducing enough Min Input Offset
		/// (recommended at least 10 ticks), while still being able to perform predictions if and when necessary.
		/// </summary>
		public bool LockstepSimulation;

		/// <summary> If the server should delta-compress inputs against previous tick-input-set (new/experimental). </summary>
		public bool InputDeltaCompression = true;

		/// <summary> How many ticks per second Quantum should execute. </summary>
		public int UpdateFPS = 60;

		/// <summary> How often we should send checksums of the frame state to the server for verification (useful during development, set to zero for release). Defined in frames. </summary>
		public int ChecksumInterval = 60;

		/// <summary> How many frames are kept in the local ring buffer on each client. Controls how much Quantum can predict into the future. Not used in lockstep mode. </summary>
		public int RollbackWindow = 60;

		/// <summary> How many frames the server will wait until it expires a frame and replaces all non-received inputs with repeated inputs or <see langword="null" />'s and sends it out to all players. </summary>
		public int InputHardTolerance = 8;

		/// <summary> How much staggering the Quantum client should apply to redundant input resends. 1 = Wait one frame, 2 = Wait two frames, etc. </summary>
		public int InputRedundancy = 3;

		/// <summary> How many frames Quantum will scan for repeatable inputs. 5 = Scan five frames forward and backwards, 10 = Scan ten frames, etc. </summary>
		public int InputRepeatMaxDistance = 10;

		/// <summary> How long the server will wait to commence the start request that was requested by the first client. This is supposed to give other clients a bit breathing room to join the room. Usually set to 0 or 1. Defined in seconds. Default is 1.</summary>
		public int SessionStartTimeout = 1;

		/// <summary> How many times per second the server will send out time correction packages to make sure every clients time is synchronized. </summary>
		public int TimeCorrectionRate = 4;

		/// <summary> How much the local client time must differ with the server time when a time correction package is received for the client to adjust it's local clock. Defined in frames. </summary>
		public int MinTimeCorrectionFrames = 1;

		/// <summary> How many frames the current local input delay must diff to the current requested offset for Quantum to update the local input offset. Defined in frames. </summary>
		public int MinOffsetCorrectionDiff = 1;

		/// <summary> The smallest timescale that can be applied by the server. Defined in percent. </summary>
		public int TimeScaleMin = 100;

		/// <summary> The ping value that the server will start lowering the time scale towards 'Time Scale Minimum'. Defined in milliseconds. </summary>
		public int TimeScalePingMin = 100;

		/// <summary> The ping value that the server will reach the 'Time Scale Minimum' value at, i.e. be at its slowest setting. Defined in milliseconds. </summary>
		public int TimeScalePingMax = 300;

		/// <summary> The minimum input offset a player can have. Defined in ticks.</summary>
		public int InputDelayMin;

		/// <summary> The maximum input offset a player can have. Defined in ticks.</summary>
		public int InputDelayMax = 60;

		/// <summary> At what ping value that Quantum starts applying input offset. Defined in milliseconds. </summary>
		public int InputDelayPingStart = 100;

		/// <summary> Fixed input size. </summary>
		public int InputFixedSize;

		/// <summary>
		/// Converts a DeterministicSessionConfig instance to a byte array.
		/// </summary>
		/// <param name="instance">The DeterministicSessionConfig instance to convert.</param>
		/// <returns>A byte array representing the serialized DeterministicSessionConfig instance.</returns>
		public static byte[] ToByteArray(DeterministicSessionConfig instance)
		{
			BitStream bitStream = new BitStream(8192);
			bitStream.Writing = true;
			Serializer serializer = new Serializer();
			serializer.ProtocolVersion = "3.0.0.0";
			Serialize(serializer, bitStream, ref instance);
			return bitStream.ToArray();
		}

		/// <summary>
		/// Converts a byte array to a DeterministicSessionConfig instance.
		/// </summary>
		/// <param name="data">The byte array to convert.</param>
		/// <returns>The deserialized DeterministicSessionConfig instance.</returns>
		public static DeterministicSessionConfig FromByteArray(byte[] data)
		{
			BitStream bitStream = new BitStream(data);
			bitStream.Reading = true;
			Serializer serializer = new Serializer();
			serializer.ProtocolVersion = "3.0.0.0";
			DeterministicSessionConfig config = null;
			Serialize(serializer, bitStream, ref config);
			return config;
		}

		/// <summary>
		/// Serializes a DeterministicSessionConfig instance to a BitStream.
		/// </summary>
		/// <param name="serializer">The serializer instance to use for serialization.</param>
		/// <param name="stream">The BitStream to write the serialized data to.</param>
		/// <param name="config">The DeterministicSessionConfig instance to serialize.</param>
		public static void Serialize(Serializer serializer, BitStream stream, ref DeterministicSessionConfig config)
		{
			if (stream.Reading)
			{
				if (stream.ReadBool())
				{
					return;
				}
				config = new DeterministicSessionConfig();
			}
			else if (stream.WriteBool(config == null))
			{
				return;
			}
			stream.Serialize(ref config.PlayerCount);
			stream.Serialize(ref config.LockstepSimulation);
			stream.Serialize(ref config.InputDeltaCompression);
			stream.Serialize(ref config.InputDelayMin);
			stream.Serialize(ref config.InputDelayMax);
			stream.Serialize(ref config.InputDelayPingStart);
			stream.Serialize(ref config.UpdateFPS);
			stream.Serialize(ref config.ChecksumInterval);
			stream.Serialize(ref config.RollbackWindow);
			stream.Serialize(ref config.InputHardTolerance);
			stream.Serialize(ref config.InputRedundancy);
			stream.Serialize(ref config.InputRepeatMaxDistance);
			stream.Serialize(ref config.SessionStartTimeout);
			stream.Serialize(ref config.TimeCorrectionRate);
			stream.Serialize(ref config.MinTimeCorrectionFrames);
			stream.Serialize(ref config.MinOffsetCorrectionDiff);
			stream.Serialize(ref config.TimeScaleMin);
			stream.Serialize(ref config.TimeScalePingMin);
			stream.Serialize(ref config.TimeScalePingMax);
			stream.Serialize(ref config.ChecksumCrossPlatformDeterminism);
			stream.Serialize(ref config.InputFixedSize);
		}
	}
}

