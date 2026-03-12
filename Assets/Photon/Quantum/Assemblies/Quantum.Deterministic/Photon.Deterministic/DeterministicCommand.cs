using System;

namespace Photon.Deterministic
{
	/// <summary>
	/// The base class for deterministic commands.
	/// Always override  <see cref="M:Photon.Deterministic.DeterministicCommand.Serialize(Photon.Deterministic.BitStream)" /> to add additional data.
	/// </summary>
	public abstract class DeterministicCommand : IDeterministicCommandFactory, IDisposable
	{
		internal byte[] Source;

		internal bool Pooled;

		/// <summary>
		/// The command pool that the object is returned to during <see cref="M:Photon.Deterministic.DeterministicCommand.Dispose" />.
		/// </summary>
		public IDeterministicCommandPool Pool;

		/// <summary>
		/// Returns the type of the concrete command class.
		/// </summary>
		public Type CommandType => GetType();

		/// <summary>
		/// Empty constructor.
		/// </summary>
		public DeterministicCommand()
		{
		}

		/// <summary>
		/// Serialized the command to and from the bitstream.
		/// Use the <see cref="M:Photon.Deterministic.DeterministicCommand.Serialize(Photon.Deterministic.BitStream)" /> method to add custom data.
		/// </summary>
		/// <param name="stream">Stream</param>
		/// <param name="cmdSerializer">Serializer</param>
		public virtual void Serialize(BitStream stream, DeterministicCommandSerializer cmdSerializer)
		{
			Serialize(stream);
		}

		/// <summary>
		/// Override this method to add additional data to the command.
		/// </summary>
		/// <param name="stream">Stream to write and read from</param>
		public abstract void Serialize(BitStream stream);

		/// <summary>
		/// The object can be used as a <see cref="T:Photon.Deterministic.IDeterministicCommandFactory" /> and it 
		/// will try to creates an instance of the command using the pool, otherwise it will use reflection.
		/// </summary>
		/// <returns></returns>
		public virtual DeterministicCommand GetCommandInstance()
		{
			if (Pool != null)
			{
				return Pool.Acquire();
			}
			return (DeterministicCommand)Activator.CreateInstance(GetType());
		}

		/// <summary>
		/// Disposing commands is done internally and does not need to be called manually.
		/// The method is public because of the <see cref="T:System.IDisposable" /> interface and to be able to derive it.
		/// </summary>
		public virtual void Dispose()
		{
			Pool?.Release(this);
		}
	}
}

