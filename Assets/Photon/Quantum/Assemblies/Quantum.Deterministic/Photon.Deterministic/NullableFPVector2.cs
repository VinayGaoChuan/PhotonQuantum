using System;
using System.Runtime.InteropServices;

namespace Photon.Deterministic
{
	/// <summary>
	/// A serializable equivalent of Nullable&lt;FPVector2&gt;.
	/// </summary>
	/// \ingroup MathAPI
	[Serializable]
	[StructLayout(LayoutKind.Explicit)]
	public struct NullableFPVector2
	{
		/// <summary>
		/// Size of the struct in bytes.
		/// </summary>
		public const int SIZE = 24;

		/// <summary>
		/// If 1, then <see cref="F:Photon.Deterministic.NullableFPVector2._value" /> is valid.
		/// </summary>
		[FieldOffset(0)]
		public long _hasValue;

		/// <summary>
		/// The value.
		/// </summary>
		[FieldOffset(8)]
		public FPVector2 _value;

		/// <summary>
		/// Returns <see langword="true" /> if this nullable has a value.
		/// </summary>
		public readonly bool HasValue => _hasValue == 1;

		/// <summary>
		/// Returns current value.
		/// </summary>
		/// <exception cref="T:System.NullReferenceException">If <see cref="P:Photon.Deterministic.NullableFPVector2.HasValue" /> is <see langword="false" /></exception>
		public readonly FPVector2 Value
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
		/// If <see cref="P:Photon.Deterministic.NullableFPVector2.HasValue" /> is <see langword="true" />, returns <see cref="P:Photon.Deterministic.NullableFPVector2.Value" />. Otherwise returns <paramref name="v" />.
		/// </summary>
		/// <param name="v"></param>
		/// <returns></returns>
		public readonly FPVector2 ValueOrDefault(FPVector2 v)
		{
			if (_hasValue != 1)
			{
				return v;
			}
			return Value;
		}

		/// <summary>
		/// Implicitly converts <paramref name="v" /> to NullableFPVector2.
		/// </summary>
		/// <param name="v"></param>
		/// <returns></returns>
		public static implicit operator NullableFPVector2(FPVector2 v)
		{
			return new NullableFPVector2
			{
				_value = v,
				_hasValue = 1L
			};
		}

		/// <summary>
		/// Serializes a NullableFPVector2 object using the given IDeterministicFrameSerializer.
		/// </summary>
		/// <param name="ptr">A pointer to the NullableFPVector2 object.</param>
		/// <param name="serializer">The IDeterministicFrameSerializer used for serialization.</param>
		public unsafe static void Serialize(NullableFPVector2* ptr, IDeterministicFrameSerializer serializer)
		{
			if (serializer.Writing)
			{
				serializer.Stream.WriteFPVector2(ptr->_value);
				serializer.Stream.WriteBoolean(ptr->_hasValue == 1);
			}
			else
			{
				ptr->_value = serializer.Stream.ReadFPVector2();
				ptr->_hasValue = (serializer.Stream.ReadBool() ? 1 : 0);
			}
		}

		/// <summary>
		/// Computes the hash code for the current NullableFPVector2 object.
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

