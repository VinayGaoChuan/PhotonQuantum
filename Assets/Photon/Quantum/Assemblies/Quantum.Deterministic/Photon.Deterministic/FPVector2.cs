using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Photon.Deterministic
{
	/// <summary>
	/// Represents a 2D Vector
	/// </summary>
	/// \ingroup MathAPI
	[Serializable]
	[StructLayout(LayoutKind.Explicit)]
	public struct FPVector2 : IEquatable<FPVector2>
	{
		/// <summary>
		/// Represents an equality comparer for FPVector2 objects.
		/// </summary>
		public class EqualityComparer : IEqualityComparer<FPVector2>
		{
			/// <summary>
			/// The global FPVector2 equality comparer instance.
			/// </summary>
			public static readonly EqualityComparer Instance = new EqualityComparer();

			private EqualityComparer()
			{
			}

			bool IEqualityComparer<FPVector2>.Equals(FPVector2 x, FPVector2 y)
			{
				return x == y;
			}

			int IEqualityComparer<FPVector2>.GetHashCode(FPVector2 obj)
			{
				return obj.GetHashCode();
			}
		}

		/// <summary>
		/// The size of the component (or struct/type) in-memory inside the Frame data-buffers or stack (when passed as value parameter).
		/// Not related to the snapshot payload this occupies, which is bit-packed and compressed.
		/// </summary>
		public const int SIZE = 16;

		/// <summary>The X component of the vector.</summary>
		[FieldOffset(0)]
		public FP X;

		/// <summary>The Y component of the vector.</summary>
		[FieldOffset(8)]
		public FP Y;

		/// <summary>
		/// A vector with components (0,0);
		/// </summary>
		public static FPVector2 Zero => default(FPVector2);

		/// <summary>
		/// A vector with components (1,1);
		/// </summary>
		public static FPVector2 One => new FPVector2
		{
			X = 
			{
				RawValue = 65536L
			},
			Y = 
			{
				RawValue = 65536L
			}
		};

		/// <summary>
		/// A vector with components (1,0);
		/// </summary>
		public static FPVector2 Right => new FPVector2
		{
			X = 
			{
				RawValue = 65536L
			}
		};

		/// <summary>
		/// A vector with components (-1,0);
		/// </summary>
		public static FPVector2 Left => new FPVector2
		{
			X = 
			{
				RawValue = -65536L
			}
		};

		/// <summary>
		/// A vector with components (0,1);
		/// </summary>
		public static FPVector2 Up => new FPVector2
		{
			Y = 
			{
				RawValue = 65536L
			}
		};

		/// <summary>
		/// A vector with components (0,-1);
		/// </summary>
		public static FPVector2 Down => new FPVector2
		{
			Y = 
			{
				RawValue = -65536L
			}
		};

		/// <summary>
		/// A vector with components 
		/// (FP.MinValue,FP.MinValue);
		/// </summary>
		public static FPVector2 MinValue => new FPVector2
		{
			X = 
			{
				RawValue = long.MinValue
			},
			Y = 
			{
				RawValue = long.MinValue
			}
		};

		/// <summary>
		/// A vector with components 
		/// (FP.MaxValue,FP.MaxValue);
		/// </summary>
		public static FPVector2 MaxValue => new FPVector2
		{
			X = 
			{
				RawValue = long.MaxValue
			},
			Y = 
			{
				RawValue = long.MaxValue
			}
		};

		/// <summary>
		/// A vector with components 
		/// (FP.UseableMin,FP.UseableMin);
		/// </summary>
		public static FPVector2 UseableMin => new FPVector2
		{
			X = 
			{
				RawValue = -2147483648L
			},
			Y = 
			{
				RawValue = -2147483648L
			}
		};

		/// <summary>
		/// A vector with components 
		/// (FP.UseableMax,FP.UseableMax);
		/// </summary>
		public static FPVector2 UseableMax => new FPVector2
		{
			X = 
			{
				RawValue = 2147483647L
			},
			Y = 
			{
				RawValue = 2147483647L
			}
		};

		/// <summary>
		/// Gets the length of the vector.
		/// </summary>
		/// <returns>Returns the length of the vector.</returns>
		public readonly FP Magnitude
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				FP result = default(FP);
				result.RawValue = FPMath.SqrtRaw((X.RawValue * X.RawValue + 32768 >> 16) + (Y.RawValue * Y.RawValue + 32768 >> 16));
				return result;
			}
		}

		/// <summary>
		/// Gets the squared length of the vector.
		/// </summary>
		/// <returns>Returns the squared length of the vector.</returns>
		public readonly FP SqrMagnitude
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				FP result = default(FP);
				result.RawValue = (X.RawValue * X.RawValue + 32768 >> 16) + (Y.RawValue * Y.RawValue + 32768 >> 16);
				return result;
			}
		}

		/// <summary>
		/// Gets a normalized version of the vector.
		/// </summary>
		/// <returns>Returns a normalized version of the vector.</returns>
		public readonly FPVector2 Normalized
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				return Normalize(this);
			}
		}

		/// <summary>
		/// Returns vector (X, 0, Y).
		/// </summary>
		public readonly FPVector3 XOY
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				return new FPVector3(X, FP._0, Y);
			}
		}

		/// <summary>
		/// Returns vector (X, Y, 0).
		/// </summary>
		public readonly FPVector3 XYO
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				return new FPVector3(X, Y, FP._0);
			}
		}

		/// <summary>
		/// Returns vector (0, X, Y).
		/// </summary>
		public readonly FPVector3 OXY
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				return new FPVector3(FP._0, X, Y);
			}
		}

		/// <summary>
		/// Returns a new FPVector3 using the X, X and X components of this vector.
		/// </summary>
		public readonly FPVector3 XXX
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				FPVector3 result = default(FPVector3);
				result.X = X;
				result.Y = X;
				result.Z = X;
				return result;
			}
		}

		/// <summary>
		/// Returns a new FPVector3 using the X, X and Y components of this vector.
		/// </summary>
		public readonly FPVector3 XXY
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				FPVector3 result = default(FPVector3);
				result.X = X;
				result.Y = X;
				result.Z = Y;
				return result;
			}
		}

		/// <summary>
		/// Returns a new FPVector3 using the X, Y and X components of this vector.
		/// </summary>
		public readonly FPVector3 XYX
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				FPVector3 result = default(FPVector3);
				result.X = X;
				result.Y = Y;
				result.Z = X;
				return result;
			}
		}

		/// <summary>
		/// Returns a new FPVector3 using the X, Y and Y components of this vector.
		/// </summary>
		public readonly FPVector3 XYY
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				FPVector3 result = default(FPVector3);
				result.X = X;
				result.Y = Y;
				result.Z = Y;
				return result;
			}
		}

		/// <summary>
		/// Returns a new FPVector2 using the X and X components of this vector.
		/// </summary>
		public readonly FPVector2 XX
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				FPVector2 result = default(FPVector2);
				result.X = X;
				result.Y = X;
				return result;
			}
		}

		/// <summary>
		/// Returns a new FPVector2 using the X and Y components of this vector.
		/// </summary>
		public readonly FPVector2 XY
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				FPVector2 result = default(FPVector2);
				result.X = X;
				result.Y = Y;
				return result;
			}
		}

		/// <summary>
		/// Returns a new FPVector3 using the Y, Y and Y components of this vector.
		/// </summary>
		public readonly FPVector3 YYY
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				FPVector3 result = default(FPVector3);
				result.X = Y;
				result.Y = Y;
				result.Z = Y;
				return result;
			}
		}

		/// <summary>
		/// Returns a new FPVector3 using the Y, Y and X components of this vector.
		/// </summary>
		public readonly FPVector3 YYX
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				FPVector3 result = default(FPVector3);
				result.X = Y;
				result.Y = Y;
				result.Z = X;
				return result;
			}
		}

		/// <summary>
		/// Returns a new FPVector3 using the Y, X and Y components of this vector.
		/// </summary>
		public readonly FPVector3 YXY
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				FPVector3 result = default(FPVector3);
				result.X = Y;
				result.Y = X;
				result.Z = Y;
				return result;
			}
		}

		/// <summary>
		/// Returns a new FPVector3 using the Y, X and X components of this vector.
		/// </summary>
		public readonly FPVector3 YXX
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				FPVector3 result = default(FPVector3);
				result.X = Y;
				result.Y = X;
				result.Z = X;
				return result;
			}
		}

		/// <summary>
		/// Returns a new FPVector2 using the Y and Y components of this vector.
		/// </summary>
		public readonly FPVector2 YY
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				FPVector2 result = default(FPVector2);
				result.X = Y;
				result.Y = Y;
				return result;
			}
		}

		/// <summary>
		/// Returns a new FPVector2 using the Y and X components of this vector.
		/// </summary>
		public readonly FPVector2 YX
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				FPVector2 result = default(FPVector2);
				result.X = Y;
				result.Y = X;
				return result;
			}
		}

		/// <summary>
		/// Normalizes the given vector. If the vector is too short to normalize, <see cref="P:Photon.Deterministic.FPVector2.Zero" /> will be returned.
		/// </summary>
		/// <param name="value">The vector which should be normalized.</param>
		/// <returns>A normalized vector.</returns>
		public static FPVector2 Normalize(FPVector2 value)
		{
			ulong num = (ulong)(value.X.RawValue * value.X.RawValue + value.Y.RawValue * value.Y.RawValue);
			if (num == 0L)
			{
				return default(FPVector2);
			}
			FPMath.ExponentMantisaPair sqrtExponentMantissa = FPMath.GetSqrtExponentMantissa(num);
			long num2 = 17592186044416L / sqrtExponentMantissa.Mantissa;
			value.X.RawValue = value.X.RawValue * num2 >> 22 + sqrtExponentMantissa.Exponent - 8;
			value.Y.RawValue = value.Y.RawValue * num2 >> 22 + sqrtExponentMantissa.Exponent - 8;
			return value;
		}

		/// <summary>
		/// Normalizes the given vector. If the vector is too short to normalize, <see cref="P:Photon.Deterministic.FPVector2.Zero" /> will be returned.
		/// </summary>
		/// <param name="value">The vector which should be normalized.</param>
		/// <param name="magnitude">The original vector's magnitude.</param>
		/// <returns>A normalized vector.</returns>
		public static FPVector2 Normalize(FPVector2 value, out FP magnitude)
		{
			ulong num = (ulong)(value.X.RawValue * value.X.RawValue + value.Y.RawValue * value.Y.RawValue);
			if (num == 0L)
			{
				magnitude.RawValue = 0L;
				return default(FPVector2);
			}
			FPMath.ExponentMantisaPair sqrtExponentMantissa = FPMath.GetSqrtExponentMantissa(num);
			long num2 = 17592186044416L / sqrtExponentMantissa.Mantissa;
			value.X.RawValue = value.X.RawValue * num2 >> 22 + sqrtExponentMantissa.Exponent - 8;
			value.Y.RawValue = value.Y.RawValue * num2 >> 22 + sqrtExponentMantissa.Exponent - 8;
			magnitude.RawValue = (long)sqrtExponentMantissa.Mantissa << sqrtExponentMantissa.Exponent;
			magnitude.RawValue >>= 14;
			return value;
		}

		/// <summary>
		/// Serializes a FPVector2 instance.
		/// </summary>
		/// <param name="ptr">A pointer to the FPVector2 instance</param>
		/// <param name="serializer">An instance of IDeterministicFrameSerializer used for serialization.</param>
		public unsafe static void Serialize(void* ptr, IDeterministicFrameSerializer serializer)
		{
			FP.Serialize(&((FPVector2*)ptr)->X, serializer);
			FP.Serialize(&((FPVector2*)ptr)->Y, serializer);
		}

		/// <summary>
		/// Initializes a new instance of the FPVector2 struct.
		/// </summary>
		/// <param name="x">The x-coordinate of the vector.</param>
		/// <param name="y">The y-coordinate of the vector.</param>
		public FPVector2(int x, int y)
		{
			X.RawValue = (long)x << 16;
			Y.RawValue = (long)y << 16;
		}

		/// <summary>
		/// Creates a new FPVector2 instance.
		/// </summary>
		/// <param name="x">X component</param>
		/// <param name="y">Y component</param>
		public FPVector2(FP x, FP y)
		{
			X = x;
			Y = y;
		}

		/// <summary>
		/// Creates a new FPVector2 instance.
		/// </summary>
		/// <param name="value">A value to be assigned to both components</param>
		public FPVector2(FP value)
		{
			X = value;
			Y = value;
		}

		/// <summary>
		/// Determines whether the current FPVector2 instance is equal to another object.
		/// </summary>
		/// <param name="obj">The object to compare with the current instance.</param>
		/// <returns><see langword="true" /> if the specified object is equal to the current FPVector2 instance; otherwise, <see langword="false" />.</returns>
		public override readonly bool Equals(object obj)
		{
			if (!(obj is FPVector2))
			{
				return false;
			}
			return this == (FPVector2)obj;
		}

		/// <summary>
		/// Determines whether an FPVector2 instance is equal to another FPVector2 instance.
		/// </summary>
		/// <param name="other">The other FPVector2 instance to compare to.</param>
		/// <returns><see langword="true" /> if the two instances are equal; otherwise, <see langword="false" />.</returns>
		public readonly bool Equals(FPVector2 other)
		{
			return this == other;
		}

		/// <summary>
		/// Computes the hash code for the current FPVector2 object.
		/// </summary>
		/// <returns>
		/// A 32-bit signed integer hash code.
		/// </returns>
		public override readonly int GetHashCode()
		{
			int num = 17;
			num = num * 31 + X.GetHashCode();
			return num * 31 + Y.GetHashCode();
		}

		/// <summary>
		/// Returns a string that represents the current FPVector2 instance.
		/// </summary>
		/// <returns>A string representation of the current FPVector2 instance.</returns>
		public override readonly string ToString()
		{
			return string.Format(CultureInfo.InvariantCulture, "({0}, {1})", X.AsFloat, Y.AsFloat);
		}

		/// <summary>
		/// Calculates the distance between two vectors
		/// </summary>
		/// <param name="a">First vector</param>
		/// <param name="b">Second vector</param>
		/// <returns>The distance between the vectors</returns>
		public static FP Distance(FPVector2 a, FPVector2 b)
		{
			long num = a.X.RawValue - b.X.RawValue;
			long num2 = a.Y.RawValue - b.Y.RawValue;
			FP result = default(FP);
			result.RawValue = FPMath.SqrtRaw((num * num + 32768 >> 16) + (num2 * num2 + 32768 >> 16));
			return result;
		}

		/// <summary>
		/// Calculates the squared distance between two vectors
		/// </summary>
		/// <param name="a">First vector</param>
		/// <param name="b">Second vector</param>
		/// <returns>The squared distance between the vectors</returns>
		public static FP DistanceSquared(FPVector2 a, FPVector2 b)
		{
			long num = a.X.RawValue - b.X.RawValue;
			long num2 = a.Y.RawValue - b.Y.RawValue;
			FP result = default(FP);
			result.RawValue = (num * num + 32768 >> 16) + (num2 * num2 + 32768 >> 16);
			return result;
		}

		/// <summary>
		/// Calculates the dot product of two vectors.
		/// </summary>
		/// <param name="a">The first vector.</param>
		/// <param name="b">The second vector.</param>
		/// <returns>Returns the dot product of both vectors.</returns>
		public static FP Dot(FPVector2 a, FPVector2 b)
		{
			long num = a.X.RawValue * b.X.RawValue + 32768 >> 16;
			long num2 = a.Y.RawValue * b.Y.RawValue + 32768 >> 16;
			FP result = default(FP);
			result.RawValue = num + num2;
			return result;
		}

		/// <summary>
		/// Clamps the magnitude of a vector
		/// </summary>
		/// <param name="vector">Vector to clamp</param>
		/// <param name="maxLength">Max length of the supplied vector</param>
		/// <returns>The resulting (potentially clamped) vector</returns>
		public static FPVector2 ClampMagnitude(FPVector2 vector, FP maxLength)
		{
			long num = (vector.X.RawValue * vector.X.RawValue + 32768 >> 16) + (vector.Y.RawValue * vector.Y.RawValue + 32768 >> 16);
			if (num <= maxLength.RawValue * maxLength.RawValue + 32768 >> 16)
			{
				return vector;
			}
			long num2 = FPMath.SqrtRaw(num);
			if (num2 <= FP.Epsilon.RawValue)
			{
				return default(FPVector2);
			}
			vector.X.RawValue = (vector.X.RawValue << 16) / num2;
			vector.Y.RawValue = (vector.Y.RawValue << 16) / num2;
			vector.X.RawValue = vector.X.RawValue * maxLength.RawValue + 32768 >> 16;
			vector.Y.RawValue = vector.Y.RawValue * maxLength.RawValue + 32768 >> 16;
			return vector;
		}

		/// <summary>
		/// Rotates each vector of <paramref name="vectors" /> by <paramref name="radians" /> radians.
		/// </summary>
		/// Rotation is counterclockwise.
		/// <param name="vectors"></param>
		/// <param name="radians"></param>
		public static void Rotate(FPVector2[] vectors, FP radians)
		{
			for (int i = 0; i < vectors.Length; i++)
			{
				vectors[i] = Rotate(vectors[i], radians);
			}
		}

		/// <summary>
		/// Rotates <paramref name="vector" /> by <paramref name="radians" /> radians.
		/// </summary>
		/// Rotation is counterclockwise.
		/// <param name="vector"></param>
		/// <param name="radians"></param>
		public static FPVector2 Rotate(FPVector2 vector, FP radians)
		{
			FPMath.SinCosRaw(radians, out var sinRaw, out var cosRaw);
			long rawValue = (vector.X.RawValue * cosRaw + 32768 >> 16) - (vector.Y.RawValue * sinRaw + 32768 >> 16);
			long rawValue2 = (vector.X.RawValue * sinRaw + 32768 >> 16) + (vector.Y.RawValue * cosRaw + 32768 >> 16);
			vector.X.RawValue = rawValue;
			vector.Y.RawValue = rawValue2;
			return vector;
		}

		/// <summary>
		/// Rotates each vector of <paramref name="vectors" /> by an angle <paramref name="sin" /> is the sine of and <paramref name="cos" /> is the cosine of.
		/// </summary>
		/// Rotation is performed counterclockwise.
		/// <param name="vectors"></param>
		/// <param name="sin"></param>
		/// <param name="cos"></param>
		public static void Rotate(FPVector2[] vectors, FP sin, FP cos)
		{
			for (int i = 0; i < vectors.Length; i++)
			{
				vectors[i] = Rotate(vectors[i], sin, cos);
			}
		}

		/// <summary>
		/// Rotates <paramref name="vector" /> by an angle <paramref name="sin" /> is the sine of and <paramref name="cos" /> is the cosine of.
		/// </summary>
		/// Rotation is performed counterclockwise.
		/// <param name="vector"></param>
		/// <param name="sin"></param>
		/// <param name="cos"></param>
		/// <returns></returns>
		public static FPVector2 Rotate(FPVector2 vector, FP sin, FP cos)
		{
			long rawValue = (vector.X.RawValue * cos.RawValue + 32768 >> 16) - (vector.Y.RawValue * sin.RawValue + 32768 >> 16);
			long rawValue2 = (vector.X.RawValue * sin.RawValue + 32768 >> 16) + (vector.Y.RawValue * cos.RawValue + 32768 >> 16);
			vector.X.RawValue = rawValue;
			vector.Y.RawValue = rawValue2;
			return vector;
		}

		/// <summary>
		/// The perp-dot product (a 2D equivalent of the 3D cross product) of two vectors.
		/// </summary>
		/// <param name="a">The first vector.</param>
		/// <param name="b">The second vector.</param>
		/// <returns>The cross product of both vectors.</returns>
		public static FP Cross(FPVector2 a, FPVector2 b)
		{
			FP result = default(FP);
			result.RawValue = (a.X.RawValue * b.Y.RawValue + 32768 >> 16) - (a.Y.RawValue * b.X.RawValue + 32768 >> 16);
			return result;
		}

		internal static long CrossRaw(FPVector2 a, FPVector2 b)
		{
			return (a.X.RawValue * b.Y.RawValue + 32768 >> 16) - (a.Y.RawValue * b.X.RawValue + 32768 >> 16);
		}

		/// <summary>
		/// Reflects a vector off the line defined by a normal.
		/// </summary>
		/// <param name="vector">Vector to be reflected.</param>
		/// <param name="normal">Normal along which the vector is reflected. Expected to be normalized.</param>
		/// <returns></returns>
		public static FPVector2 Reflect(FPVector2 vector, FPVector2 normal)
		{
			FP fP = default(FP);
			fP.RawValue = (vector.X.RawValue * normal.X.RawValue + 32768 >> 15) + (vector.Y.RawValue * normal.Y.RawValue + 32768 >> 15);
			vector.X.RawValue -= fP.RawValue * normal.X.RawValue + 32768 >> 16;
			vector.Y.RawValue -= fP.RawValue * normal.Y.RawValue + 32768 >> 16;
			return vector;
		}

		/// <summary>
		/// Clamps each component of <paramref name="value" /> to the range [<paramref name="min" />, <paramref name="max" />]
		/// </summary>
		/// <param name="value"></param>
		/// <param name="min"></param>
		/// <param name="max"></param>
		/// <returns></returns>
		public static FPVector2 Clamp(FPVector2 value, FPVector2 min, FPVector2 max)
		{
			return new FPVector2(FPMath.Clamp(value.X, min.X, max.X), FPMath.Clamp(value.Y, min.Y, max.Y));
		}

		/// <summary>
		/// Linearly interpolates between <paramref name="start" /> and <paramref name="end" /> by <paramref name="t" />.
		/// <paramref name="t" /> is clamped to the range [0, 1]
		/// </summary>
		/// <param name="start"></param>
		/// <param name="end"></param>
		/// <param name="t"></param>
		/// <returns></returns>
		public static FPVector2 Lerp(FPVector2 start, FPVector2 end, FP t)
		{
			if (t.RawValue < 0)
			{
				t.RawValue = 0L;
			}
			if (t.RawValue > 65536)
			{
				t.RawValue = 65536L;
			}
			start.X.RawValue += (end.X.RawValue - start.X.RawValue) * t.RawValue + 32768 >> 16;
			start.Y.RawValue += (end.Y.RawValue - start.Y.RawValue) * t.RawValue + 32768 >> 16;
			return start;
		}

		/// <summary>
		/// Linearly interpolates between <paramref name="start" /> and <paramref name="end" /> by <paramref name="t" />.
		/// </summary>
		/// <param name="start"></param>
		/// <param name="end"></param>
		/// <param name="t"></param>
		/// <returns></returns>
		public static FPVector2 LerpUnclamped(FPVector2 start, FPVector2 end, FP t)
		{
			start.X.RawValue += (end.X.RawValue - start.X.RawValue) * t.RawValue + 32768 >> 16;
			start.Y.RawValue += (end.Y.RawValue - start.Y.RawValue) * t.RawValue + 32768 >> 16;
			return start;
		}

		/// <summary>
		/// Gets a vector with the maximum x and y values of both vectors.
		/// </summary>
		/// <param name="value1">The first value.</param>
		/// <param name="value2">The second value.</param>
		/// <returns>A vector with the maximum x and y values of both vectors.</returns>
		public static FPVector2 Max(FPVector2 value1, FPVector2 value2)
		{
			return new FPVector2(FPMath.Max(value1.X, value2.X), FPMath.Max(value1.Y, value2.Y));
		}

		/// <summary>
		/// Gets a vector with the maximum x and y values of all the vectors. If
		/// <paramref name="vectors" /> is <see langword="null" /> or empty, return <see cref="P:Photon.Deterministic.FPVector2.Zero" />.
		/// </summary>
		/// <param name="vectors"></param>
		/// <returns></returns>
		public static FPVector2 Max(params FPVector2[] vectors)
		{
			if (vectors == null || vectors.Length == 0)
			{
				return default(FPVector2);
			}
			FPVector2 result = vectors[0];
			for (int i = 1; i < vectors.Length; i++)
			{
				result.X.RawValue = Math.Max(result.X.RawValue, vectors[i].X.RawValue);
				result.Y.RawValue = Math.Max(result.Y.RawValue, vectors[i].Y.RawValue);
			}
			return result;
		}

		/// <summary>
		/// Gets a vector with the minimum x and y values of both vectors.
		/// </summary>
		/// <param name="value1">The first value.</param>
		/// <param name="value2">The second value.</param>
		/// <returns>A vector with the minimum x and y values of both vectors.</returns>
		public static FPVector2 Min(FPVector2 value1, FPVector2 value2)
		{
			return new FPVector2(FPMath.Min(value1.X, value2.X), FPMath.Min(value1.Y, value2.Y));
		}

		/// <summary>
		/// Gets a vector with the min x and y values of all the vectors. If
		/// <paramref name="vectors" /> is <see langword="null" /> or empty, return <see cref="P:Photon.Deterministic.FPVector2.Zero" />.
		/// </summary>
		/// <param name="vectors"></param>
		/// <returns></returns>
		public static FPVector2 Min(params FPVector2[] vectors)
		{
			if (vectors == null || vectors.Length == 0)
			{
				return default(FPVector2);
			}
			FPVector2 result = vectors[0];
			for (int i = 1; i < vectors.Length; i++)
			{
				result.X.RawValue = Math.Min(result.X.RawValue, vectors[i].X.RawValue);
				result.Y.RawValue = Math.Min(result.Y.RawValue, vectors[i].Y.RawValue);
			}
			return result;
		}

		/// <summary>
		/// Multiplies each component of the vector by the same components of the provided vector.
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static FPVector2 Scale(FPVector2 a, FPVector2 b)
		{
			FPVector2 result = default(FPVector2);
			result.X = a.X * b.X;
			result.Y = a.Y * b.Y;
			return result;
		}

		/// <summary>
		/// Returns the angle in degrees between <paramref name="a" /> and <paramref name="b" />.
		/// <remarks>
		/// See also: <see cref="M:Photon.Deterministic.FPVector2.Radians(Photon.Deterministic.FPVector2,Photon.Deterministic.FPVector2)" />, <seealso cref="M:Photon.Deterministic.FPVector2.RadiansSigned(Photon.Deterministic.FPVector2,Photon.Deterministic.FPVector2)" />, <seealso cref="M:Photon.Deterministic.FPVector2.RadiansSkipNormalize(Photon.Deterministic.FPVector2,Photon.Deterministic.FPVector2)" />, <seealso cref="M:Photon.Deterministic.FPVector2.RadiansSignedSkipNormalize(Photon.Deterministic.FPVector2,Photon.Deterministic.FPVector2)" />,
		/// </remarks>
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static FP Angle(FPVector2 a, FPVector2 b)
		{
			long num = (a.X.RawValue * a.X.RawValue + 32768 >> 16) + (a.Y.RawValue * a.Y.RawValue + 32768 >> 16);
			if (num == 0L)
			{
				return default(FP);
			}
			num = FPMath.SqrtRaw(num);
			a.X.RawValue = (a.X.RawValue << 16) / num;
			a.Y.RawValue = (a.Y.RawValue << 16) / num;
			num = (b.X.RawValue * b.X.RawValue + 32768 >> 16) + (b.Y.RawValue * b.Y.RawValue + 32768 >> 16);
			if (num == 0L)
			{
				return default(FP);
			}
			num = FPMath.SqrtRaw(num);
			b.X.RawValue = (b.X.RawValue << 16) / num;
			b.Y.RawValue = (b.Y.RawValue << 16) / num;
			num = (a.X.RawValue * b.X.RawValue + 32768 >> 16) + (a.Y.RawValue * b.Y.RawValue + 32768 >> 16);
			num = ((num < -65536) ? FPLut.acos_lut[0] : ((num <= 65536) ? FPLut.acos_lut[num + 65536] : FPLut.acos_lut[131072]));
			a.X.RawValue = num * FP.Rad2Deg.RawValue + 32768 >> 16;
			return a.X;
		}

		/// <summary>
		/// Returns vector rotated by 90 degrees clockwise.
		/// </summary>
		/// <param name="vector"></param>
		/// <returns></returns>
		public static FPVector2 CalculateRight(FPVector2 vector)
		{
			return new FPVector2(vector.Y, -vector.X);
		}

		/// <summary>
		/// Returns vector rotated by 90 degrees counterclockwise.
		/// </summary>
		/// <param name="vector"></param>
		/// <returns></returns>
		public static FPVector2 CalculateLeft(FPVector2 vector)
		{
			return new FPVector2(-vector.Y, vector.X);
		}

		/// <summary>
		/// Returns <see langword="true" /> if this vector is on the right side of <paramref name="vector" />
		/// </summary>
		/// <param name="vector"></param>
		/// <returns></returns>
		public readonly bool IsRightOf(FPVector2 vector)
		{
			return Dot(CalculateRight(vector), this) > FP._0;
		}

		/// <summary>
		/// Returns <see langword="true" /> if this vector is on the left side of <paramref name="vector" />
		/// </summary>
		/// <param name="vector"></param>
		/// <returns></returns>
		public readonly bool IsLeftOf(FPVector2 vector)
		{
			return Dot(CalculateLeft(vector), this) > FP._0;
		}

		/// <summary>
		/// Returns determinant of two 2d vectors which is handy to check the angle between them (method is identical to FPVector2.Cross).
		/// Determinant == 0 -&gt; Vector1 and Vector2 are collinear 
		/// Determinant less than 0 -&gt; Vector1 is left of Vector2
		/// Determinant greater than 0 -&gt; Vector1 is right of Vector2
		/// </summary>
		/// <param name="v1">Vector1</param>
		/// <param name="v2">Vector2</param>
		/// <returns>Determinant</returns>    
		public static FP Determinant(FPVector2 v1, FPVector2 v2)
		{
			return new FP
			{
				RawValue = (v1.X.RawValue * v2.Y.RawValue + 32768 >> 16) - (v1.Y.RawValue * v2.X.RawValue + 32768 >> 16)
			};
		}

		/// <summary>
		/// Returns radians between two vectors.
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static FP Radians(FPVector2 a, FPVector2 b)
		{
			FP value = Dot(Normalize(a), Normalize(b));
			return FPMath.Acos(FPMath.Clamp(value, -FP._1, FP._1));
		}

		/// <summary>
		/// Returns radians between two vectors. Vectors are assumed to be normalized.
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static FP RadiansSkipNormalize(FPVector2 a, FPVector2 b)
		{
			return FPMath.Acos(FPMath.Clamp(Dot(a, b), -FP._1, FP._1));
		}

		/// <summary>
		/// Returns radians between two vectors. The result will be a negative number if 
		/// <paramref name="b" /> is on the right side of <paramref name="a" />.
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static FP RadiansSigned(FPVector2 a, FPVector2 b)
		{
			FP fP = Radians(a, b);
			FP fP2 = FPMath.Sign(a.X * b.Y - a.Y * b.X);
			return fP * fP2;
		}

		/// <summary>
		/// Returns radians between two vectors. The result will be a negative number if 
		/// <paramref name="b" /> is on the right side of <paramref name="a" />. Vectors are assumed to be normalized.
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static FP RadiansSignedSkipNormalize(FPVector2 a, FPVector2 b)
		{
			FP fP = RadiansSkipNormalize(a, b);
			FP fP2 = FPMath.Sign(a.X * b.Y - a.Y * b.X);
			return fP * fP2;
		}

		/// <summary>
		/// Interpolates between <paramref name="start" /> and <paramref name="end" /> with smoothing at the limits.
		/// Equivalent of calling <see cref="M:Photon.Deterministic.FPMath.SmoothStep(Photon.Deterministic.FP,Photon.Deterministic.FP,Photon.Deterministic.FP)" /> for each component pair. 
		/// </summary>
		/// <param name="start"></param>
		/// <param name="end"></param>
		/// <param name="t"></param>
		/// <returns></returns>
		public static FPVector2 SmoothStep(FPVector2 start, FPVector2 end, FP t)
		{
			return new FPVector2(FPMath.SmoothStep(start.X, end.X, t), FPMath.SmoothStep(start.Y, end.Y, t));
		}

		/// <summary>
		/// Equivalent of calling<see cref="M:Photon.Deterministic.FPMath.Hermite(Photon.Deterministic.FP,Photon.Deterministic.FP,Photon.Deterministic.FP,Photon.Deterministic.FP,Photon.Deterministic.FP)" /> for each component.
		/// </summary>
		/// <param name="value1"></param>
		/// <param name="tangent1"></param>
		/// <param name="value2"></param>
		/// <param name="tangent2"></param>
		/// <param name="t"></param>
		/// <returns></returns>
		public static FPVector2 Hermite(FPVector2 value1, FPVector2 tangent1, FPVector2 value2, FPVector2 tangent2, FP t)
		{
			FPVector2 result = default(FPVector2);
			result.X = FPMath.Hermite(value1.X, tangent1.X, value2.X, tangent2.X, t);
			result.Y = FPMath.Hermite(value1.Y, tangent1.Y, value2.Y, tangent2.Y, t);
			return result;
		}

		/// <summary>
		/// Equivalent of calling <see cref="M:Photon.Deterministic.FPMath.Barycentric(Photon.Deterministic.FP,Photon.Deterministic.FP,Photon.Deterministic.FP,Photon.Deterministic.FP,Photon.Deterministic.FP)" /> for each component.
		/// </summary>
		/// <param name="value1"></param>
		/// <param name="value2"></param>
		/// <param name="value3"></param>
		/// <param name="t1"></param>
		/// <param name="t2"></param>
		/// <returns></returns>
		public static FPVector2 Barycentric(FPVector2 value1, FPVector2 value2, FPVector2 value3, FP t1, FP t2)
		{
			return new FPVector2(FPMath.Barycentric(value1.X, value2.X, value3.X, t1, t2), FPMath.Barycentric(value1.Y, value2.Y, value3.Y, t1, t2));
		}

		/// <summary>
		/// Equivalent of calling <see cref="M:Photon.Deterministic.FPMath.CatmullRom(Photon.Deterministic.FP,Photon.Deterministic.FP,Photon.Deterministic.FP,Photon.Deterministic.FP,Photon.Deterministic.FP)" /> for each component.
		/// </summary>
		/// <param name="value1"></param>
		/// <param name="value2"></param>
		/// <param name="value3"></param>
		/// <param name="value4"></param>
		/// <param name="t"></param>
		/// <returns></returns>
		public static FPVector2 CatmullRom(FPVector2 value1, FPVector2 value2, FPVector2 value3, FPVector2 value4, FP t)
		{
			return new FPVector2(FPMath.CatmullRom(value1.X, value2.X, value3.X, value4.X, t), FPMath.CatmullRom(value1.Y, value2.Y, value3.Y, value4.Y, t));
		}

		/// <summary>
		/// Returns <see langword="true" /> if the polygon defined by <paramref name="vertices" /> is convex.
		/// </summary>
		/// <param name="vertices"></param>
		/// <returns></returns>
		public static bool IsPolygonConvex(FPVector2[] vertices)
		{
			bool flag = false;
			bool flag2 = false;
			for (int i = 0; i < vertices.Length; i++)
			{
				int num = (i + 1) % vertices.Length;
				int num2 = (num + 1) % vertices.Length;
				FP fP = CrossProductLength(vertices[i], vertices[num], vertices[num2]);
				if (fP < 0)
				{
					flag = true;
				}
				else if (fP > 0)
				{
					flag2 = true;
				}
				if (flag && flag2)
				{
					return false;
				}
			}
			return true;
		}

		private static FP CrossProductLength(FPVector2 A, FPVector2 B, FPVector2 C)
		{
			FP fP = A.X - B.X;
			FP fP2 = A.Y - B.Y;
			FP fP3 = C.X - B.X;
			FP fP4 = C.Y - B.Y;
			return fP * fP4 - fP2 * fP3;
		}

		/// <summary>
		/// Checks if the vertices of a polygon are clock-wise
		/// </summary>
		/// <param name="vertices">The vertices of the polygon</param>
		/// <returns><see langword="true" /> if the vertices are clock-wise aligned.</returns>
		public static bool IsClockWise(FPVector2[] vertices)
		{
			FPVector2 fPVector = new FPVector2(vertices[1].X - vertices[0].X, vertices[1].Y - vertices[0].Y);
			FPVector2 fPVector2 = new FPVector2(vertices[2].X - vertices[1].X, vertices[2].Y - vertices[1].Y);
			return fPVector.X * fPVector2.Y - fPVector.Y * fPVector2.X < 0;
		}

		/// <summary>
		/// Checks if the vertices of a polygon are counter clock-wise.
		/// </summary>
		/// <param name="vertices">The vertices of the polygon</param>
		/// <returns><see langword="true" /> if the vertices are counter clock-wise aligned.</returns>
		public static bool IsCounterClockWise(FPVector2[] vertices)
		{
			return !IsClockWise(vertices);
		}

		/// <summary>
		/// Checks if the vertices of a polygon are clock-wise if not makes them counter clock-wise.
		/// </summary>
		/// <param name="vertices">The vertices of the polygon</param>
		public static void MakeCounterClockWise(FPVector2[] vertices)
		{
			if (IsClockWise(vertices))
			{
				FlipWindingOrder(vertices);
			}
		}

		/// <summary>
		/// Flips the winding order of the vertices if they are in counter clock-wise order. This ensures that the vertices are in clock-wise order.
		/// </summary>
		/// <param name="vertices">The array of vertices</param>
		public static void MakeClockWise(FPVector2[] vertices)
		{
			if (IsCounterClockWise(vertices))
			{
				FlipWindingOrder(vertices);
			}
		}

		/// <summary>
		/// Reverses the order of vertices in an array, effectively flipping the winding order of a polygon.
		/// </summary>
		/// <param name="vertices">The array of vertices representing the polygon</param>
		public static void FlipWindingOrder(FPVector2[] vertices)
		{
			Array.Reverse(vertices);
		}

		/// <summary>
		/// Calculates a normal for each edge of a polygon defined by <paramref name="vertices" />.
		/// </summary>
		/// <param name="vertices"></param>
		/// <returns></returns>
		public static FPVector2[] CalculatePolygonNormals(FPVector2[] vertices)
		{
			FPVector2[] array = new FPVector2[vertices.Length];
			for (int i = 0; i < vertices.Length; i++)
			{
				int num = ((i + 1 < vertices.Length) ? (i + 1) : 0);
				FPVector2 fPVector = vertices[num] - vertices[i];
				if (fPVector.X.RawValue == 0L)
				{
					_ = fPVector.Y.RawValue;
				}
				array[i] = new FPVector2(fPVector.Y, -fPVector.X);
				array[i] = array[i].Normalized;
			}
			return array;
		}

		/// <summary>
		/// Returns <see langword="true" /> if all normals of a polygon defined by <paramref name="vertices" /> are non-zeroed.
		/// </summary>
		/// <param name="vertices"></param>
		/// <returns></returns>
		public static bool PolygonNormalsAreValid(FPVector2[] vertices)
		{
			for (int i = 0; i < vertices.Length; i++)
			{
				int num = (i + 1) % vertices.Length;
				if (vertices[num].X.RawValue == vertices[i].X.RawValue && vertices[num].Y.RawValue == vertices[i].Y.RawValue)
				{
					return false;
				}
			}
			return true;
		}

		/// <summary>
		/// Shifts polygon defined by <paramref name="vertices" /> so that (0,0) becomes its center.
		/// </summary>
		/// <param name="vertices"></param>
		/// <returns></returns>
		public static FPVector2[] RecenterPolygon(FPVector2[] vertices)
		{
			FPVector2 fPVector = CalculatePolygonCentroid(vertices);
			FPVector2[] array = (FPVector2[])vertices.Clone();
			for (int i = 0; i < vertices.Length; i++)
			{
				array[i] = vertices[i] - fPVector;
			}
			return array;
		}

		/// <summary>
		/// Retruns an area of a polygon defined by <paramref name="vertices" />.
		/// </summary>
		/// <param name="vertices"></param>
		/// <returns></returns>
		public static FP CalculatePolygonArea(FPVector2[] vertices)
		{
			FP _ = FP._0;
			for (int i = 0; i < vertices.Length; i++)
			{
				FPVector2 fPVector = vertices[(i + 1) % vertices.Length];
				_ += vertices[i].X * fPVector.Y - vertices[i].Y * fPVector.X;
			}
			return _ / 2;
		}

		/// <summary>
		/// Returns a centroid of a polygon defined by <paramref name="vertices" />.
		/// </summary>
		/// <param name="vertices"></param>
		/// <returns></returns>
		public static FPVector2 CalculatePolygonCentroid(FPVector2[] vertices)
		{
			FPVector2 result = default(FPVector2);
			FP _ = FP._0;
			FP _2 = FP._0;
			FP _3 = FP._0;
			FP _4 = FP._0;
			FP _5 = FP._0;
			FP _6 = FP._0;
			for (int i = 0; i < vertices.Length; i++)
			{
				_2 = vertices[i].X;
				_3 = vertices[i].Y;
				_4 = vertices[(i + 1) % vertices.Length].X;
				_5 = vertices[(i + 1) % vertices.Length].Y;
				_6 = _2 * _5 - _4 * _3;
				_ += _6;
				result.X += (_2 + _4) * _6;
				result.Y += (_3 + _5) * _6;
			}
			if (_ == FP._0)
			{
				throw new ArgumentException("Not a valid polygon");
			}
			_ *= FP._0_50;
			result.X /= FP._6 * _;
			result.Y /= FP._6 * _;
			return result;
		}

		/// <summary>
		/// Calculates the mass moment of inertia factor of a polygon defined by <paramref name="vertices" />.
		/// </summary>
		/// <remarks>To compute a body mass moment of inertia, multiply the factor by the body mass.</remarks>
		/// <param name="vertices">The 2D vertices that define the polygon.</param>
		/// <returns>The mass moment of inertia factor of the polygon.</returns>
		public static FP CalculatePolygonInertiaFactor(FPVector2[] vertices)
		{
			FP result = default(FP);
			long num = 0L;
			for (int i = 0; i < vertices.Length; i++)
			{
				FPVector2 fPVector = vertices[i];
				FPVector2 fPVector2 = vertices[(i + 1) % vertices.Length];
				long num2 = (fPVector.X.RawValue * fPVector2.Y.RawValue + 32768 >> 16) - (fPVector.Y.RawValue * fPVector2.X.RawValue + 32768 >> 16);
				num += num2;
				long num3 = (fPVector.X.RawValue * fPVector.X.RawValue + 32768 >> 16) + (fPVector.Y.RawValue * fPVector.Y.RawValue + 32768 >> 16);
				long num4 = (fPVector2.X.RawValue * fPVector2.X.RawValue + 32768 >> 16) + (fPVector2.Y.RawValue * fPVector2.Y.RawValue + 32768 >> 16);
				long num5 = (fPVector.X.RawValue * fPVector2.X.RawValue + 32768 >> 16) + (fPVector.Y.RawValue * fPVector2.Y.RawValue + 32768 >> 16);
				result.RawValue += num2 * (num3 + num4 + num5) + 32768 >> 16;
			}
			result.RawValue = (result.RawValue << 16) / (num * FP._6.RawValue + 32768 >> 16);
			return result;
		}

		/// <summary>
		/// Calculates the support point in a direction <paramref name="localDir" /> of a polygon defined by <paramref name="vertices" />.
		/// <remarks>A support point is the furthest point of a shape in a given direction.</remarks>
		/// <remarks>Both support point and direction are expressed in the local space of the polygon.</remarks>
		/// <remarks>The polygon vertices are expected to be counterclockwise.</remarks>
		/// </summary>
		/// <param name="vertices">The 2D vertices that define the polygon.</param>
		/// <param name="localDir">The direction, in local space, in which the support point will be calculated.</param>
		/// <returns>The support point, in local space.</returns>
		public static FPVector2 CalculatePolygonLocalSupport(FPVector2[] vertices, ref FPVector2 localDir)
		{
			FPVector2 fPVector = vertices[0];
			FPVector2 result = fPVector;
			long rawValue = Dot(fPVector, localDir).RawValue;
			fPVector = vertices[1];
			long rawValue2 = Dot(fPVector, localDir).RawValue;
			if (rawValue2 > rawValue)
			{
				result = fPVector;
				rawValue = rawValue2;
				for (int i = 2; i < vertices.Length; i++)
				{
					fPVector = vertices[i];
					rawValue2 = Dot(fPVector, localDir).RawValue;
					if (rawValue2 <= rawValue)
					{
						break;
					}
					result = fPVector;
					rawValue = rawValue2;
				}
			}
			else
			{
				fPVector = vertices[^1];
				rawValue2 = Dot(fPVector, localDir).RawValue;
				if (rawValue2 > rawValue)
				{
					result = fPVector;
					rawValue = rawValue2;
					for (int num = vertices.Length - 2; num > 1; num--)
					{
						fPVector = vertices[num];
						rawValue2 = Dot(fPVector, localDir).RawValue;
						if (rawValue2 <= rawValue)
						{
							break;
						}
						result = fPVector;
						rawValue = rawValue2;
					}
				}
			}
			return result;
		}

		/// <summary>
		/// Calculates the local support point of a polygon in a given direction.
		/// </summary>
		/// <param name="vertices">An array of vertices that make up the polygon.</param>
		/// <param name="verticesCount">The number of vertices in the polygon.</param>
		/// <param name="localDir">The direction for which to find the local support point.</param>
		/// <returns>The local support point of the polygon in the given direction.</returns>
		public unsafe static FPVector2 CalculatePolygonLocalSupport(FPVector2* vertices, int verticesCount, ref FPVector2 localDir)
		{
			FPVector2 fPVector = *vertices;
			FPVector2 result = fPVector;
			long rawValue = Dot(fPVector, localDir).RawValue;
			fPVector = vertices[1];
			long rawValue2 = Dot(fPVector, localDir).RawValue;
			if (rawValue2 > rawValue)
			{
				result = fPVector;
				rawValue = rawValue2;
				for (int i = 2; i < verticesCount; i++)
				{
					fPVector = vertices[i];
					rawValue2 = Dot(fPVector, localDir).RawValue;
					if (rawValue2 <= rawValue)
					{
						break;
					}
					result = fPVector;
					rawValue = rawValue2;
				}
			}
			else
			{
				fPVector = vertices[verticesCount - 1];
				rawValue2 = Dot(fPVector, localDir).RawValue;
				if (rawValue2 > rawValue)
				{
					result = fPVector;
					rawValue = rawValue2;
					for (int num = verticesCount - 2; num > 1; num--)
					{
						fPVector = vertices[num];
						rawValue2 = Dot(fPVector, localDir).RawValue;
						if (rawValue2 <= rawValue)
						{
							break;
						}
						result = fPVector;
						rawValue = rawValue2;
					}
				}
			}
			return result;
		}

		/// <summary>
		/// Returns a radius of a centered polygon defined by <paramref name="vertices" />.
		/// </summary>
		/// <param name="vertices"></param>
		/// <returns></returns>
		public static FP CalculatePolygonRadius(FPVector2[] vertices)
		{
			FP fP = FP._0;
			for (int i = 0; i < vertices.Length; i++)
			{
				FP magnitude = vertices[i].Magnitude;
				if (magnitude > fP)
				{
					fP = magnitude;
				}
			}
			return fP;
		}

		/// <summary>
		/// Calculate a position between the points specified by <paramref name="from" /> and <paramref name="to" />, moving no farther than the distance specified by <paramref name="maxDelta" />.
		/// </summary>
		/// <param name="from"></param>
		/// <param name="to"></param>
		/// <param name="maxDelta"></param>
		/// <returns></returns>s
		public static FPVector2 MoveTowards(FPVector2 from, FPVector2 to, FP maxDelta)
		{
			FPVector2 fPVector = to - from;
			FP magnitude = fPVector.Magnitude;
			if (magnitude.RawValue <= maxDelta.RawValue || magnitude.RawValue == 0L)
			{
				return to;
			}
			return from + fPVector / magnitude * maxDelta;
		}

		/// <summary>
		/// Returns <see langword="true" /> if two vectors are exactly equal.
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator ==(FPVector2 a, FPVector2 b)
		{
			if (a.X.RawValue == b.X.RawValue)
			{
				return a.Y.RawValue == b.Y.RawValue;
			}
			return false;
		}

		/// <summary>
		/// Returns <see langword="true" /> if two vectors are not exactly equal.
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator !=(FPVector2 a, FPVector2 b)
		{
			if (a.X.RawValue == b.X.RawValue)
			{
				return a.Y.RawValue != b.Y.RawValue;
			}
			return true;
		}

		/// <summary>
		/// Negates each component of <paramref name="v" /> vector.
		/// </summary>
		/// <param name="v"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static FPVector2 operator -(FPVector2 v)
		{
			v.X.RawValue = -v.X.RawValue;
			v.Y.RawValue = -v.Y.RawValue;
			return v;
		}

		/// <summary>
		/// Adds two vectors.
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static FPVector2 operator +(FPVector2 a, FPVector2 b)
		{
			a.X.RawValue += b.X.RawValue;
			a.Y.RawValue += b.Y.RawValue;
			return a;
		}

		/// <summary>
		/// Subtracts <paramref name="b" /> from <paramref name="a" />
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static FPVector2 operator -(FPVector2 a, FPVector2 b)
		{
			a.X.RawValue -= b.X.RawValue;
			a.Y.RawValue -= b.Y.RawValue;
			return a;
		}

		/// <summary>
		/// Multiplies each component of <paramref name="v" /> times <paramref name="s" />.
		/// </summary>
		/// <param name="v"></param>
		/// <param name="s"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static FPVector2 operator *(FPVector2 v, FP s)
		{
			v.X.RawValue = v.X.RawValue * s.RawValue + 32768 >> 16;
			v.Y.RawValue = v.Y.RawValue * s.RawValue + 32768 >> 16;
			return v;
		}

		/// <summary>
		/// Multiplies each component of <paramref name="v" /> times <paramref name="s" />.
		/// </summary>
		/// <param name="v"></param>
		/// <param name="s"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static FPVector2 operator *(FP s, FPVector2 v)
		{
			v.X.RawValue = v.X.RawValue * s.RawValue + 32768 >> 16;
			v.Y.RawValue = v.Y.RawValue * s.RawValue + 32768 >> 16;
			return v;
		}

		/// <summary>
		/// Multiplies each component of <paramref name="v" /> times <paramref name="s" />.
		/// </summary>
		/// <param name="v"></param>
		/// <param name="s"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static FPVector2 operator *(FPVector2 v, int s)
		{
			v.X.RawValue = v.X.RawValue * s;
			v.Y.RawValue = v.Y.RawValue * s;
			return v;
		}

		/// <summary>
		/// Multiplies each component of <paramref name="v" /> times <paramref name="s" />.
		/// </summary>
		/// <param name="v"></param>
		/// <param name="s"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static FPVector2 operator *(int s, FPVector2 v)
		{
			v.X.RawValue = v.X.RawValue * s;
			v.Y.RawValue = v.Y.RawValue * s;
			return v;
		}

		/// <summary>
		/// Divides each component of <paramref name="v" /> by <paramref name="s" />.
		/// </summary>
		/// <param name="v"></param>
		/// <param name="s"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static FPVector2 operator /(FPVector2 v, FP s)
		{
			v.X.RawValue = (v.X.RawValue << 16) / s.RawValue;
			v.Y.RawValue = (v.Y.RawValue << 16) / s.RawValue;
			return v;
		}

		/// <summary>
		/// Divides each component of <paramref name="v" /> by <paramref name="s" />.
		/// </summary>
		/// <param name="v"></param>
		/// <param name="s"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static FPVector2 operator /(FPVector2 v, int s)
		{
			v.X.RawValue = v.X.RawValue / s;
			v.Y.RawValue = v.Y.RawValue / s;
			return v;
		}
	}
}

