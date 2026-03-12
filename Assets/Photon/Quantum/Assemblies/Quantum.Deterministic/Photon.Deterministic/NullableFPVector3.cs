using System;
using System.Runtime.InteropServices;

namespace Photon.Deterministic
{
	/// <summary>
	/// A serializable equivalent of Nullable&lt;FPVector3&gt;.
	/// </summary>
	/// \ingroup MathAPI
	[Serializable]
	[StructLayout(LayoutKind.Explicit)]
	public struct NullableFPVector3
	{
		/// <summary>
		/// Size of the struct in bytes.
		/// </summary>
		public const int SIZE = 32;

		/// <summary>
		/// If 1, then <see cref="F:Photon.Deterministic.NullableFPVector3._value" /> is valid.
		/// </summary>
		[FieldOffset(0)]
		public long _hasValue;

		/// <summary>
		/// The value.
		/// </summary>
		[FieldOffset(8)]
		public FPVector3 _value;

		/// <summary>
		/// Returns <see langword="true" /> if this nullable has a value.
		/// </summary>
		public readonly bool HasValue => _hasValue == 1;

		/// <summary>
		/// Returns current value.
		/// </summary>
		/// <exception cref="T:System.NullReferenceException">If <see cref="P:Photon.Deterministic.NullableFPVector3.HasValue" /> is <see langword="false" /></exception>
		public readonly FPVector3 Value
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
		/// If <see cref="P:Photon.Deterministic.NullableFPVector3.HasValue" /> is <see langword="true" />, returns <see cref="P:Photon.Deterministic.NullableFPVector3.Value" />. Otherwise returns <paramref name="v" />.
		/// </summary>
		/// <param name="v"></param>
		/// <returns></returns>
		public readonly FPVector3 ValueOrDefault(FPVector3 v)
		{
			if (_hasValue != 1)
			{
				return v;
			}
			return Value;
		}

		/// <summary>
		/// Implicitly converts an FPVector3 to a NullableFPVector3.
		/// </summary>
		/// <param name="v">The FPVector3 to convert.</param>
		/// <returns>A NullableFPVector3 instance with the converted value.</returns>
		public static implicit operator NullableFPVector3(FPVector3 v)
		{
			return new NullableFPVector3
			{
				_value = v,
				_hasValue = 1L
			};
		}

		/// <summary>
		/// Serialize the data of a NullableFPVector3 object using the provided serializer.
		/// </summary>
		/// <param name="ptr">A pointer to the NullableFPVector3 object to be serialized.</param>
		/// <param name="serializer">The serializer object used to perform the serialization.</param>
		public unsafe static void Serialize(NullableFPVector3* ptr, IDeterministicFrameSerializer serializer)
		{
			if (serializer.Writing)
			{
				serializer.Stream.WriteFPVector3(ptr->_value);
				serializer.Stream.WriteBoolean(ptr->_hasValue == 1);
			}
			else
			{
				ptr->_value = serializer.Stream.ReadFPVector3();
				ptr->_hasValue = (serializer.Stream.ReadBool() ? 1 : 0);
			}
		}

		/// <summary>
		/// Gets the hash code of the NullableFPVector3 instance.
		/// </summary>
		/// <returns>The hash code of the NullableFPVector3.</returns>
		/// <remarks>
		/// If <see cref="P:Photon.Deterministic.NullableFPVector3.HasValue" /> is <see langword="false" />, the hash code is always 0.
		/// If <see cref="P:Photon.Deterministic.NullableFPVector3.HasValue" /> is <see langword="true" />, the hash code is calculated based on the value of <see cref="P:Photon.Deterministic.NullableFPVector3.Value" />.
		/// </remarks>
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

