using System;
using System.Runtime.InteropServices;

namespace Photon.Deterministic
{
	/// <summary>
	/// A serializable equivalent of Nullable&lt;FP&gt;.
	/// </summary>
	/// \ingroup MathAPI
	[Serializable]
	[StructLayout(LayoutKind.Explicit)]
	public struct NullableFP
	{
		/// <summary>
		/// Size of the struct in bytes.
		/// </summary>
		public const int SIZE = 16;

		/// <summary>
		/// If 1, then <see cref="F:Photon.Deterministic.NullableFP._value" /> is valid.
		/// </summary>
		[FieldOffset(0)]
		public long _hasValue;

		/// <summary>
		/// The value.
		/// </summary>
		[FieldOffset(8)]
		public FP _value;

		/// <summary>
		/// Returns <see langword="true" /> if this nullable has a value.
		/// </summary>
		public readonly bool HasValue => _hasValue == 1;

		/// <summary>
		/// Returns current value.
		/// </summary>
		/// <exception cref="T:System.NullReferenceException">If <see cref="P:Photon.Deterministic.NullableFP.HasValue" /> is <see langword="false" /></exception>
		public readonly FP Value
		{
			get
			{
				if (_hasValue == 0L)
				{
					throw new NullReferenceException();
				}
				return _value;
			}
		}

		/// <summary>
		/// If <see cref="P:Photon.Deterministic.NullableFP.HasValue" /> is <see langword="true" />, returns <see cref="P:Photon.Deterministic.NullableFP.Value" />. Otherwise returns <paramref name="v" />.
		/// </summary>
		/// <param name="v"></param>
		/// <returns></returns>
		public readonly FP ValueOrDefault(FP v)
		{
			if (_hasValue != 1)
			{
				return v;
			}
			return Value;
		}

		/// <summary>
		/// Converts <paramref name="v" /> to NullableFP.
		/// </summary>
		/// <param name="v"></param>
		/// <returns></returns>
		public static implicit operator NullableFP(FP v)
		{
			return new NullableFP
			{
				_value = v,
				_hasValue = 1L
			};
		}

		/// <summary>
		/// Serializes a NullableFP object using the given serializer.
		/// </summary>
		/// <param name="ptr">A pointer to the NullableFP object to be serialized.</param>
		/// <param name="serializer">The serializer object used for serialization.</param>
		public unsafe static void Serialize(NullableFP* ptr, IDeterministicFrameSerializer serializer)
		{
			if (serializer.Writing)
			{
				serializer.Stream.WriteFP(ptr->_value);
				serializer.Stream.WriteBoolean(ptr->_hasValue == 1);
			}
			else
			{
				ptr->_value = serializer.Stream.ReadFP();
				ptr->_hasValue = (serializer.Stream.ReadBool() ? 1 : 0);
			}
		}

		/// <summary>
		/// Computes the hash code for the current instance of the NullableFP struct.
		/// </summary>
		/// <returns>
		/// A 32-bit signed integer hash code.
		/// </returns>
		public override readonly int GetHashCode()
		{
			if (_hasValue == 0L)
			{
				return 0;
			}
			return _value.GetHashCode();
		}
	}
}

