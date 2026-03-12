using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Photon.Deterministic
{
	/// <summary>
	/// A serializable equivalent of Nullable&lt;FP&gt;.
	/// </summary>
	/// \ingroup MathAPI
	[Serializable]
	[StructLayout(LayoutKind.Explicit)]
	public struct NullableNonNegativeFP
	{
		/// <summary>
		/// Size of the struct in bytes.
		/// </summary>
		public const int SIZE = 8;

		/// <summary>
		/// The value.
		/// </summary>
		[FieldOffset(0)]
		public ulong _value;

		private const ulong HasValueBit = 9223372036854775808uL;

		private const ulong ValueMask = 9223372036854775807uL;

		/// <summary>
		/// Returns <see langword="true" /> if this nullable has a value.
		/// </summary>
		public readonly bool HasValue
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				return (_value & 0x8000000000000000uL) != 0;
			}
		}

		/// <summary>
		/// Returns current value.
		/// </summary>
		/// <exception cref="T:System.NullReferenceException">If <see cref="P:Photon.Deterministic.NullableNonNegativeFP.HasValue" /> is <see langword="false" /></exception>
		public readonly FP Value
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				if (!HasValue)
				{
					throw new NullReferenceException();
				}
				FP result = default(FP);
				result.RawValue = (long)(_value & 0x7FFFFFFFFFFFFFFFL);
				return result;
			}
		}

		/// <summary>
		/// If <see cref="P:Photon.Deterministic.NullableNonNegativeFP.HasValue" /> is <see langword="true" />, returns <see cref="P:Photon.Deterministic.NullableNonNegativeFP.Value" />. Otherwise returns zero.
		/// </summary>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly FP ValueOrDefault()
		{
			FP result = default(FP);
			result.RawValue = (long)(_value & 0x7FFFFFFFFFFFFFFFL);
			return result;
		}

		/// <summary>
		/// Implicitly converts the specified value of type FP to NullableNonNegativeFP.
		/// </summary>
		/// <param name="v">The value to be converted.</param>
		/// <returns>A NullableNonNegativeFP representing the converted value.</returns>
		/// <exception cref="T:System.ArgumentOutOfRangeException">Thrown when the value is less than zero.</exception>
		public static implicit operator NullableNonNegativeFP(FP v)
		{
			if (v < 0)
			{
				throw new ArgumentOutOfRangeException($"Non negative values only allowed: {v}");
			}
			NullableNonNegativeFP result = default(NullableNonNegativeFP);
			result._value = (ulong)(v.RawValue | long.MinValue);
			return result;
		}

		/// <summary>
		/// Serialize a NullableNonNegativeFP pointer using the provided IDeterministicFrameSerializer.
		/// </summary>
		/// <param name="ptr">The NullableNonNegativeFP pointer to serialize.</param>
		/// <param name="serializer">The IDeterministicFrameSerializer used for serialization.</param>
		public unsafe static void Serialize(NullableNonNegativeFP* ptr, IDeterministicFrameSerializer serializer)
		{
			serializer.Stream.Serialize(&ptr->_value);
		}

		/// <summary>
		/// Returns a string representation of the current value.
		/// </summary>
		/// <returns></returns>
		public override readonly string ToString()
		{
			if (!HasValue)
			{
				return "NULL";
			}
			return Value.ToString();
		}

		/// <summary>
		/// Computes the hash code for the current instance of the FP struct.
		/// </summary>
		/// <returns>A 32-bit signed integer hash code.</returns>
		public override readonly int GetHashCode()
		{
			if (!HasValue)
			{
				return 0;
			}
			return Value.GetHashCode();
		}
	}
}

