using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Photon.Deterministic;

namespace Quantum
{
	/// <summary>
	/// Represents a Quantum player.
	/// <para>The PlayerRef, in contrast to the player index, is 1-based. The reason is that default(PlayerRef) will return a "null/invalid" player ref struct for convenience. There are automatic cast operators that can cast an int into a PlayerRef.</para>
	/// </summary>
	/// <example>
	/// default(PlayerRef), internally a 0, means NOBODY
	/// PlayerRef, internally 1, is the same as player index 0
	/// PlayerRef, internally 2, is the same as player index 1
	/// </example>
	[Serializable]
	[StructLayout(LayoutKind.Explicit)]
	public struct PlayerRef : IEquatable<PlayerRef>
	{
		/// <summary>
		/// The size of the struct in-memory inside the Frame data-buffers or stack (when passed as value parameter).
		/// Not related to the snapshot payload this occupies, which is bit-packed and compressed.
		/// </summary>
		public const int SIZE = 4;

		/// <summary>
		/// PlayerRef backing field. 0 means no player.
		/// </summary>
		[FieldOffset(0)]
		public int _index;

		/// <summary>
		/// None player has index 0.
		/// </summary>
		public static PlayerRef None
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				return default(PlayerRef);
			}
		}

		/// <summary>
		/// Returns <see langword="true" /> if the player reference is valid (larger than 0).
		/// </summary>
		public readonly bool IsValid => _index > 0;

		/// <summary>
		/// Returns <see langword="true" /> if the PlayerRefs are equal.
		/// </summary>
		public readonly bool Equals(PlayerRef other)
		{
			return _index == other._index;
		}

		/// <summary>
		/// Returns <see langword="true" /> if the PlayerRefs are equal.
		/// </summary>
		public override readonly bool Equals(object obj)
		{
			if (obj is PlayerRef playerRef)
			{
				return playerRef.Equals(this);
			}
			return false;
		}

		/// <summary>
		/// Overrides the default hash function.
		/// </summary>
		/// <returns>A hash code for the current object.</returns>
		public override readonly int GetHashCode()
		{
			return _index;
		}

		/// <summary>
		/// Converts the numeric value of this instance to its equivalent string representation.
		/// </summary>
		/// <returns>The string representation of the value of this instance</returns>
		public override readonly string ToString()
		{
			if (_index > 0)
			{
				return $"[Player:{_index - 1}]";
			}
			return "[Player:None]";
		}

		/// <summary>
		/// Converts a PlayerRef to an integer.
		/// <para>The PlayerRef is 1-based and it will return 0 for player 1 for example.</para>
		/// </summary>
		/// <param name="value">PlayerRef to cast to int.</param>
		public static implicit operator int(PlayerRef value)
		{
			return value._index - 1;
		}

		/// <summary>
		/// Converts an integer to a PlayerRef.
		/// <para>The PlayerRef is 1-based and will return PlayerRef 1 for input 0 for example.</para>
		/// </summary>
		/// <param name="value">The integer to cast into a PlayerRef.</param>
		public static implicit operator PlayerRef(int value)
		{
			PlayerRef result = default(PlayerRef);
			result._index = value + 1;
			return result;
		}

		/// <summary>
		/// Operator override for which checks if two instances of PlayerRef are equal.
		/// </summary>
		/// <returns><see langword="true" /> if the instances are equal.</returns>
		public static bool operator ==(PlayerRef a, PlayerRef b)
		{
			return a._index == b._index;
		}

		/// <summary>
		/// Operator override for which checks if two instances of PlayerRef are not equal.
		/// </summary>
		/// <returns><see langword="true" /> if the instances are not equal.</returns>
		public static bool operator !=(PlayerRef a, PlayerRef b)
		{
			return a._index != b._index;
		}

		/// <summary>
		/// Serializes a <see cref="T:Quantum.PlayerRef" /> into a <see cref="T:Photon.Deterministic.IDeterministicFrameSerializer" /> to write or read from a frame snapshot.
		/// </summary>
		/// <param name="ptr">The pointer to the <see cref="T:Quantum.PlayerRef" />.</param>
		/// <param name="serializer">The <see cref="T:Photon.Deterministic.IDeterministicFrameSerializer" /> instance into which the struct will be serialized.</param>
		public unsafe static void Serialize(void* ptr, IDeterministicFrameSerializer serializer)
		{
			if (serializer.Writing)
			{
				serializer.Stream.WriteInt(((PlayerRef*)ptr)->_index);
			}
			else
			{
				((PlayerRef*)ptr)->_index = serializer.Stream.ReadInt();
			}
		}
	}
}

