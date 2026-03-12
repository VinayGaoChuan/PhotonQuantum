using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Photon.Deterministic
{
	/// <summary>
	/// Represents a 3D Vector
	/// </summary>
	/// \ingroup MathAPI
	[Serializable]
	[StructLayout(LayoutKind.Explicit)]
	public struct FPVector3 : IEquatable<FPVector3>
	{
		/// <summary>
		/// Represents an equality comparer for FPVector3 objects.
		/// </summary>
		public class EqualityComparer : IEqualityComparer<FPVector3>
		{
			public static readonly EqualityComparer Instance = new EqualityComparer();

			private EqualityComparer()
			{
			}

			bool IEqualityComparer<FPVector3>.Equals(FPVector3 x, FPVector3 y)
			{
				return x == y;
			}

			int IEqualityComparer<FPVector3>.GetHashCode(FPVector3 obj)
			{
				return obj.GetHashCode();
			}
		}

		/// <summary>
		/// The size of the vector (3 FP values).
		/// </summary>
		public const int SIZE = 24;

		/// <summary>The X component of the vector.</summary>
		[FieldOffset(0)]
		public FP X;

		/// <summary>The Y component of the vector.</summary>
		[FieldOffset(8)]
		public FP Y;

		/// <summary>The Z component of the vector.</summary>
		[FieldOffset(16)]
		public FP Z;

		/// <summary>
		/// A vector with components (0,0,0);
		/// </summary>
		public static FPVector3 Zero => default(FPVector3);

		/// <summary>
		/// A vector with components (-1,0,0);
		/// </summary>
		public static FPVector3 Left => new FPVector3
		{
			X = 
			{
				RawValue = -65536L
			}
		};

		/// <summary>
		/// A vector with components (1,0,0);
		/// </summary>
		public static FPVector3 Right => new FPVector3
		{
			X = 
			{
				RawValue = 65536L
			}
		};

		/// <summary>
		/// A vector with components (0,1,0);
		/// </summary>
		public static FPVector3 Up => new FPVector3
		{
			Y = 
			{
				RawValue = 65536L
			}
		};

		/// <summary>
		/// A vector with components (0,-1,0);
		/// </summary>
		public static FPVector3 Down => new FPVector3
		{
			Y = 
			{
				RawValue = -65536L
			}
		};

		/// <summary>
		/// A vector with components (0,0,-1);
		/// </summary>
		public static FPVector3 Back => new FPVector3
		{
			Z = 
			{
				RawValue = -65536L
			}
		};

		/// <summary>
		/// A vector with components (0,0,1);
		/// </summary>
		public static FPVector3 Forward => new FPVector3
		{
			Z = 
			{
				RawValue = 65536L
			}
		};

		/// <summary>
		/// A vector with components (1,1,1);
		/// </summary>
		public static FPVector3 One => new FPVector3
		{
			X = 
			{
				RawValue = 65536L
			},
			Y = 
			{
				RawValue = 65536L
			},
			Z = 
			{
				RawValue = 65536L
			}
		};

		/// <summary>
		/// A vector with components 
		/// (FP.MinValue,FP.MinValue,FP.MinValue);
		/// </summary>
		public static FPVector3 MinValue => new FPVector3
		{
			X = 
			{
				RawValue = long.MinValue
			},
			Y = 
			{
				RawValue = long.MinValue
			},
			Z = 
			{
				RawValue = long.MinValue
			}
		};

		/// <summary>
		/// A vector with components 
		/// (FP.MaxValue,FP.MaxValue,FP.MaxValue);
		/// </summary>
		public static FPVector3 MaxValue => new FPVector3
		{
			X = 
			{
				RawValue = long.MaxValue
			},
			Y = 
			{
				RawValue = long.MaxValue
			},
			Z = 
			{
				RawValue = long.MaxValue
			}
		};

		/// <summary>
		/// A vector with components 
		/// (FP.UseableMin,FP.UseableMin,FP.UseableMin);
		/// </summary>
		public static FPVector3 UseableMin => new FPVector3
		{
			X = 
			{
				RawValue = -2147483648L
			},
			Y = 
			{
				RawValue = -2147483648L
			},
			Z = 
			{
				RawValue = -2147483648L
			}
		};

		/// <summary>
		/// A vector with components 
		/// (FP.UseableMax,FP.UseableMax,FP.UseableMax);
		/// </summary>
		public static FPVector3 UseableMax => new FPVector3
		{
			X = 
			{
				RawValue = 2147483647L
			},
			Y = 
			{
				RawValue = 2147483647L
			},
			Z = 
			{
				RawValue = 2147483647L
			}
		};

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
				result.RawValue = (X.RawValue * X.RawValue + 32768 >> 16) + (Y.RawValue * Y.RawValue + 32768 >> 16) + (Z.RawValue * Z.RawValue + 32768 >> 16);
				return result;
			}
		}

		/// <summary>
		/// Gets the length of the vector.
		/// </summary>
		/// <returns>Returns the length of the vector.</returns>
		public readonly FP Magnitude
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				return FPMath.Sqrt(SqrMagnitude);
			}
		}

		/// <summary>
		/// Gets a normalized version of the vector.
		/// </summary>
		/// <returns>Returns a normalized version of the vector.</returns>
		public readonly FPVector3 Normalized
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				return Normalize(this);
			}
		}

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

		public readonly FPVector3 XXZ
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				FPVector3 result = default(FPVector3);
				result.X = X;
				result.Y = X;
				result.Z = Z;
				return result;
			}
		}

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

		public readonly FPVector3 XYZ
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				FPVector3 result = default(FPVector3);
				result.X = X;
				result.Y = Y;
				result.Z = Z;
				return result;
			}
		}

		public readonly FPVector3 XZX
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				FPVector3 result = default(FPVector3);
				result.X = X;
				result.Y = Z;
				result.Z = X;
				return result;
			}
		}

		public readonly FPVector3 XZY
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				FPVector3 result = default(FPVector3);
				result.X = X;
				result.Y = Z;
				result.Z = Y;
				return result;
			}
		}

		public readonly FPVector3 XZZ
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				FPVector3 result = default(FPVector3);
				result.X = X;
				result.Y = Z;
				result.Z = Z;
				return result;
			}
		}

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

		public readonly FPVector2 XZ
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				FPVector2 result = default(FPVector2);
				result.X = X;
				result.Y = Z;
				return result;
			}
		}

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

		public readonly FPVector3 YYZ
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				FPVector3 result = default(FPVector3);
				result.X = Y;
				result.Y = Y;
				result.Z = Z;
				return result;
			}
		}

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

		public readonly FPVector3 YZY
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				FPVector3 result = default(FPVector3);
				result.X = Y;
				result.Y = Z;
				result.Z = Y;
				return result;
			}
		}

		public readonly FPVector3 YZZ
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				FPVector3 result = default(FPVector3);
				result.X = Y;
				result.Y = Z;
				result.Z = Z;
				return result;
			}
		}

		public readonly FPVector3 YZX
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				FPVector3 result = default(FPVector3);
				result.X = Y;
				result.Y = Z;
				result.Z = X;
				return result;
			}
		}

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

		public readonly FPVector3 YXZ
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				FPVector3 result = default(FPVector3);
				result.X = Y;
				result.Y = X;
				result.Z = Z;
				return result;
			}
		}

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

		public readonly FPVector2 YZ
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				FPVector2 result = default(FPVector2);
				result.X = Y;
				result.Y = Z;
				return result;
			}
		}

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

		public readonly FPVector3 ZZZ
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				FPVector3 result = default(FPVector3);
				result.X = Z;
				result.Y = Z;
				result.Z = Z;
				return result;
			}
		}

		public readonly FPVector3 ZZX
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				FPVector3 result = default(FPVector3);
				result.X = Z;
				result.Y = Z;
				result.Z = X;
				return result;
			}
		}

		public readonly FPVector3 ZZY
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				FPVector3 result = default(FPVector3);
				result.X = Z;
				result.Y = Z;
				result.Z = Y;
				return result;
			}
		}

		public readonly FPVector3 ZXZ
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				FPVector3 result = default(FPVector3);
				result.X = Z;
				result.Y = X;
				result.Z = Z;
				return result;
			}
		}

		public readonly FPVector3 ZXX
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				FPVector3 result = default(FPVector3);
				result.X = Z;
				result.Y = X;
				result.Z = X;
				return result;
			}
		}

		public readonly FPVector3 ZXY
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				FPVector3 result = default(FPVector3);
				result.X = Z;
				result.Y = X;
				result.Z = Y;
				return result;
			}
		}

		public readonly FPVector3 ZYZ
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				FPVector3 result = default(FPVector3);
				result.X = Z;
				result.Y = Y;
				result.Z = Z;
				return result;
			}
		}

		public readonly FPVector3 ZYX
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				FPVector3 result = default(FPVector3);
				result.X = Z;
				result.Y = Y;
				result.Z = X;
				return result;
			}
		}

		public readonly FPVector3 ZYY
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				FPVector3 result = default(FPVector3);
				result.X = Z;
				result.Y = Y;
				result.Z = Y;
				return result;
			}
		}

		public readonly FPVector2 ZZ
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				FPVector2 result = default(FPVector2);
				result.X = Z;
				result.Y = Z;
				return result;
			}
		}

		public readonly FPVector2 ZX
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				FPVector2 result = default(FPVector2);
				result.X = Z;
				result.Y = X;
				return result;
			}
		}

		public readonly FPVector2 ZY
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				FPVector2 result = default(FPVector2);
				result.X = Z;
				result.Y = Y;
				return result;
			}
		}

		public readonly FPVector3 XYO
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				FPVector3 result = default(FPVector3);
				result.X = X;
				result.Y = Y;
				result.Z = default(FP);
				return result;
			}
		}

		public readonly FPVector3 XOZ
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				FPVector3 result = default(FPVector3);
				result.X = X;
				result.Y = default(FP);
				result.Z = Z;
				return result;
			}
		}

		public readonly FPVector3 OYZ
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				FPVector3 result = default(FPVector3);
				result.X = default(FP);
				result.Y = Y;
				result.Z = Z;
				return result;
			}
		}

		/// <summary>
		/// Normalizes the given vector. If the vector is too short to normalize, <see cref="P:Photon.Deterministic.FPVector3.Zero" /> will be returned.
		/// </summary>
		/// <param name="value">The vector which should be normalized.</param>
		/// <returns>A normalized vector.</returns>
		public static FPVector3 Normalize(FPVector3 value)
		{
			ulong num = (ulong)(value.X.RawValue * value.X.RawValue + value.Y.RawValue * value.Y.RawValue + value.Z.RawValue * value.Z.RawValue);
			if (num == 0L)
			{
				return default(FPVector3);
			}
			FPMath.ExponentMantisaPair sqrtExponentMantissa = FPMath.GetSqrtExponentMantissa(num);
			long num2 = 17592186044416L / sqrtExponentMantissa.Mantissa;
			value.X.RawValue = value.X.RawValue * num2 >> 22 + sqrtExponentMantissa.Exponent - 8;
			value.Y.RawValue = value.Y.RawValue * num2 >> 22 + sqrtExponentMantissa.Exponent - 8;
			value.Z.RawValue = value.Z.RawValue * num2 >> 22 + sqrtExponentMantissa.Exponent - 8;
			return value;
		}

		/// <summary>
		/// Normalizes the given vector. If the vector is too short to normalize, <see cref="P:Photon.Deterministic.FPVector3.Zero" /> will be returned.
		/// </summary>
		/// <param name="value">The vector which should be normalized.</param>
		/// <param name="magnitude">The original vector's magnitude.</param>
		/// <returns>A normalized vector.</returns>
		public static FPVector3 Normalize(FPVector3 value, out FP magnitude)
		{
			ulong num = (ulong)(value.X.RawValue * value.X.RawValue + value.Y.RawValue * value.Y.RawValue + value.Z.RawValue * value.Z.RawValue);
			if (num == 0L)
			{
				magnitude.RawValue = 0L;
				return default(FPVector3);
			}
			FPMath.ExponentMantisaPair sqrtExponentMantissa = FPMath.GetSqrtExponentMantissa(num);
			long num2 = 17592186044416L / sqrtExponentMantissa.Mantissa;
			value.X.RawValue = value.X.RawValue * num2 >> 22 + sqrtExponentMantissa.Exponent - 8;
			value.Y.RawValue = value.Y.RawValue * num2 >> 22 + sqrtExponentMantissa.Exponent - 8;
			value.Z.RawValue = value.Z.RawValue * num2 >> 22 + sqrtExponentMantissa.Exponent - 8;
			magnitude.RawValue = (long)sqrtExponentMantissa.Mantissa << sqrtExponentMantissa.Exponent;
			magnitude.RawValue >>= 14;
			return value;
		}

		public unsafe static void Serialize(void* ptr, IDeterministicFrameSerializer serializer)
		{
			FP.Serialize(&((FPVector3*)ptr)->X, serializer);
			FP.Serialize(&((FPVector3*)ptr)->Y, serializer);
			FP.Serialize(&((FPVector3*)ptr)->Z, serializer);
		}

		/// <summary>
		/// Constructor initializing a new instance of the structure
		/// </summary>
		/// <param name="x">The X component of the vector.</param>
		/// <param name="y">The Y component of the vector.</param>
		/// <param name="z">The Z component of the vector.</param>
		public FPVector3(int x, int y, int z)
		{
			X.RawValue = (long)x << 16;
			Y.RawValue = (long)y << 16;
			Z.RawValue = (long)z << 16;
		}

		/// <summary>
		/// Constructor initializing a new instance of the structure
		/// </summary>
		/// <param name="x">The X component of the vector.</param>
		/// <param name="y">The Y component of the vector.</param>
		public FPVector3(int x, int y)
		{
			X.RawValue = (long)x << 16;
			Y.RawValue = (long)y << 16;
			Z = FP._0;
		}

		/// <summary>
		/// Constructor initializing a new instance of the structure
		/// </summary>
		/// <param name="x">The X component of the vector.</param>
		/// <param name="y">The Y component of the vector.</param>
		/// <param name="z">The Z component of the vector.</param>
		public FPVector3(FP x, FP y, FP z)
		{
			X = x;
			Y = y;
			Z = z;
		}

		/// <summary>
		/// Constructor initializing a new instance of the structure
		/// </summary>
		/// <param name="x">The X component of the vector.</param>
		/// <param name="y">The Y component of the vector.</param>
		public FPVector3(FP x, FP y)
		{
			X = x;
			Y = y;
			Z = FP._0;
		}

		/// <summary>
		/// Builds a string from the FPVector3.
		/// </summary>
		/// <returns>A string containing all three components.</returns>
		public override readonly string ToString()
		{
			return string.Format(CultureInfo.InvariantCulture, "({0}, {1}, {2})", X.AsFloat, Y.AsFloat, Z.AsFloat);
		}

		/// <summary>
		/// Tests if an object is equal to this vector.
		/// </summary>
		/// <param name="obj">The object to test.</param>
		/// <returns>Returns <see langword="true" /> if they are euqal, otherwise <see langword="false" />.</returns>
		public override readonly bool Equals(object obj)
		{
			if (obj is FPVector3)
			{
				return this == (FPVector3)obj;
			}
			return false;
		}

		/// <summary>
		/// Determines whether the current instance is equal to the specified FPVector3.
		/// </summary>
		/// <param name="other">The FPVector3 to compare with the current instance.</param>
		/// <returns>
		/// <see langword="true" /> if the current instance is equal to the specified FPVector3; otherwise, <see langword="false" />.
		/// </returns>
		public readonly bool Equals(FPVector3 other)
		{
			return this == other;
		}

		/// <summary>
		/// Gets the hashcode of the vector.
		/// </summary>
		/// <returns>Returns the hashcode of the vector.</returns>
		public override readonly int GetHashCode()
		{
			int num = 17;
			num = num * 31 + X.GetHashCode();
			num = num * 31 + Y.GetHashCode();
			return num * 31 + Z.GetHashCode();
		}

		/// <summary>
		/// Returns a vector where each component is the absolute value of same component in <paramref name="value" />.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static FPVector3 Abs(FPVector3 value)
		{
			long num = value.X.RawValue >> 63;
			value.X.RawValue = (value.X.RawValue + num) ^ num;
			num = value.Y.RawValue >> 63;
			value.Y.RawValue = (value.Y.RawValue + num) ^ num;
			num = value.Z.RawValue >> 63;
			value.Z.RawValue = (value.Z.RawValue + num) ^ num;
			return value;
		}

		/// <summary>
		/// Linearly interpolates between <paramref name="start" /> and <paramref name="end" /> by <paramref name="t" />.
		/// <paramref name="t" /> is clamped to the range [0, 1]
		/// </summary>
		/// <param name="start"></param>
		/// <param name="end"></param>
		/// <param name="t"></param>
		/// <returns></returns>
		public static FPVector3 Lerp(FPVector3 start, FPVector3 end, FP t)
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
			start.Z.RawValue += (end.Z.RawValue - start.Z.RawValue) * t.RawValue + 32768 >> 16;
			return start;
		}

		/// <summary>
		/// Linearly interpolates between <paramref name="start" /> and <paramref name="end" /> by <paramref name="t" />.
		/// </summary>
		/// <param name="start"></param>
		/// <param name="end"></param>
		/// <param name="t"></param>
		/// <returns></returns>
		public static FPVector3 LerpUnclamped(FPVector3 start, FPVector3 end, FP t)
		{
			start.X.RawValue += (end.X.RawValue - start.X.RawValue) * t.RawValue + 32768 >> 16;
			start.Y.RawValue += (end.Y.RawValue - start.Y.RawValue) * t.RawValue + 32768 >> 16;
			start.Z.RawValue += (end.Z.RawValue - start.Z.RawValue) * t.RawValue + 32768 >> 16;
			return start;
		}

		/// <summary>
		/// Spherically interpolates between <paramref name="from" /> and <paramref name="to" /> by <paramref name="t" />,
		/// which is clamped to the range [0, 1].
		/// </summary>
		/// <remarks>Input vectors are normalized and treated as directions.
		/// The resultant vector has direction spherically interpolated using the angle and magnitude linearly interpolated between the magnitudes of <paramref name="to" /> and <paramref name="from" />.</remarks>
		/// <param name="from"></param>
		/// <param name="to"></param>
		/// <param name="t"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static FPVector3 Slerp(FPVector3 from, FPVector3 to, FP t)
		{
			if (t.RawValue < 0)
			{
				t.RawValue = 0L;
			}
			else if (t.RawValue > 65536)
			{
				t.RawValue = 65536L;
			}
			return SlerpUnclamped(from, to, t);
		}

		/// <summary>
		/// Spherically interpolates between <paramref name="from" /> and <paramref name="to" /> by <paramref name="t" />.
		/// </summary>
		/// <remarks>Input vectors are normalized and treated as directions.
		/// The resultant vector has direction spherically interpolated using the angle and magnitude linearly interpolated between the magnitudes of <paramref name="to" /> and <paramref name="from" />.</remarks>
		/// <param name="from"></param>
		/// <param name="to"></param>
		/// <param name="t"></param>
		/// <returns></returns>
		public static FPVector3 SlerpUnclamped(FPVector3 from, FPVector3 to, FP t)
		{
			FP magnitude;
			FPVector3 fPVector = Normalize(from, out magnitude);
			if (magnitude.RawValue == 0L)
			{
				FPVector3 result = default(FPVector3);
				result.X.RawValue = to.X.RawValue * t.RawValue + 32768 >> 16;
				result.Y.RawValue = to.Y.RawValue * t.RawValue + 32768 >> 16;
				result.Z.RawValue = to.Z.RawValue * t.RawValue + 32768 >> 16;
				return result;
			}
			FP magnitude2;
			FPVector3 b = Normalize(to, out magnitude2);
			magnitude.RawValue += (magnitude2.RawValue - magnitude.RawValue) * t.RawValue + 32768 >> 16;
			FP rad = default(FP);
			rad.RawValue = (fPVector.X.RawValue * b.X.RawValue + 32768 >> 16) + (fPVector.Y.RawValue * b.Y.RawValue + 32768 >> 16) + (fPVector.Z.RawValue * b.Z.RawValue + 32768 >> 16);
			if (rad.RawValue < -65534)
			{
				rad.RawValue = -65536L;
			}
			else if (rad.RawValue > 65534)
			{
				rad.RawValue = 65536L;
			}
			FP rad2 = default(FP);
			rad2.RawValue = FPLut.acos_lut[rad.RawValue + 65536];
			FPMath.SinCosHighPrecision(rad2, out var sin, out var cos);
			if (Math.Abs(rad.RawValue) >= 65534)
			{
				if (cos.RawValue > 0)
				{
					fPVector.X.RawValue = fPVector.X.RawValue * magnitude.RawValue + 32768 >> 16;
					fPVector.Y.RawValue = fPVector.Y.RawValue * magnitude.RawValue + 32768 >> 16;
					fPVector.Z.RawValue = fPVector.Z.RawValue * magnitude.RawValue + 32768 >> 16;
					return fPVector;
				}
				long num = b.Z.RawValue * b.Z.RawValue + 32768 >> 16;
				if (num == 65536)
				{
					b.X.RawValue = 0L;
					b.Y.RawValue = fPVector.Z.RawValue;
					b.Z.RawValue = -fPVector.Y.RawValue;
				}
				else
				{
					b.X.RawValue = fPVector.Y.RawValue;
					b.Y.RawValue = -fPVector.X.RawValue;
					b.Z.RawValue = 0L;
				}
				b = Cross(fPVector, b).Normalized;
				t.RawValue <<= 1;
				sin.RawValue = 65536L;
				cos.RawValue = 0L;
				rad2.RawValue = FPLut.acos_lut[65536];
			}
			rad.RawValue = t.RawValue * rad2.RawValue + 32768 >> 16;
			FPMath.SinCosRaw(rad, out var sinRaw, out var cosRaw);
			rad.RawValue = (sin.RawValue * cosRaw + 32768 >> 16) - (cos.RawValue * sinRaw + 32768 >> 16);
			fPVector.X.RawValue = (fPVector.X.RawValue * rad.RawValue + b.X.RawValue * sinRaw) / sin.RawValue;
			fPVector.Y.RawValue = (fPVector.Y.RawValue * rad.RawValue + b.Y.RawValue * sinRaw) / sin.RawValue;
			fPVector.Z.RawValue = (fPVector.Z.RawValue * rad.RawValue + b.Z.RawValue * sinRaw) / sin.RawValue;
			fPVector.X.RawValue = fPVector.X.RawValue * magnitude.RawValue + 32768 >> 16;
			fPVector.Y.RawValue = fPVector.Y.RawValue * magnitude.RawValue + 32768 >> 16;
			fPVector.Z.RawValue = fPVector.Z.RawValue * magnitude.RawValue + 32768 >> 16;
			return fPVector;
		}

		/// <summary>
		/// Multiplies each component of the vector by the same components of the provided vector.
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static FPVector3 Scale(FPVector3 a, FPVector3 b)
		{
			a.X.RawValue = a.X.RawValue * b.X.RawValue + 32768 >> 16;
			a.Y.RawValue = a.Y.RawValue * b.Y.RawValue + 32768 >> 16;
			a.Z.RawValue = a.Z.RawValue * b.Z.RawValue + 32768 >> 16;
			return a;
		}

		/// <summary>
		/// Clamps the magnitude of a vector
		/// </summary>
		/// <param name="vector">Vector to clamp</param>
		/// <param name="maxLength">Max length of the supplied vector</param>
		/// <returns>The resulting (potentially clamped) vector</returns>
		public static FPVector3 ClampMagnitude(FPVector3 vector, FP maxLength)
		{
			long num = maxLength.RawValue * maxLength.RawValue + 32768 >> 16;
			long num2 = (vector.X.RawValue * vector.X.RawValue + 32768 >> 16) + (vector.Y.RawValue * vector.Y.RawValue + 32768 >> 16) + (vector.Z.RawValue * vector.Z.RawValue + 32768 >> 16);
			if (num2 > num)
			{
				vector = Normalize(vector);
				vector.X.RawValue = vector.X.RawValue * maxLength.RawValue + 32768 >> 16;
				vector.Y.RawValue = vector.Y.RawValue * maxLength.RawValue + 32768 >> 16;
				vector.Z.RawValue = vector.Z.RawValue * maxLength.RawValue + 32768 >> 16;
			}
			return vector;
		}

		/// <summary>
		/// Gets a vector with the minimum x,y and z values of both vectors.
		/// </summary>
		/// <param name="value1">The first value.</param>
		/// <param name="value2">The second value.</param>
		/// <returns>A vector with the minimum x,y and z values of both vectors.</returns>
		public static FPVector3 Min(FPVector3 value1, FPVector3 value2)
		{
			value1.X = ((value1.X.RawValue < value2.X.RawValue) ? value1.X : value2.X);
			value1.Y = ((value1.Y.RawValue < value2.Y.RawValue) ? value1.Y : value2.Y);
			value1.Z = ((value1.Z.RawValue < value2.Z.RawValue) ? value1.Z : value2.Z);
			return value1;
		}

		/// <summary>
		/// Gets a vector with the maximum x,y and z values of both vectors.
		/// </summary>
		/// <param name="value1">The first value.</param>
		/// <param name="value2">The second value.</param>
		/// <returns>A vector with the maximum x,y and z values of both vectors.</returns>
		public static FPVector3 Max(FPVector3 value1, FPVector3 value2)
		{
			value1.X = ((value1.X.RawValue > value2.X.RawValue) ? value1.X : value2.X);
			value1.Y = ((value1.Y.RawValue > value2.Y.RawValue) ? value1.Y : value2.Y);
			value1.Z = ((value1.Z.RawValue > value2.Z.RawValue) ? value1.Z : value2.Z);
			return value1;
		}

		/// <summary>
		/// Calculates the distance between two vectors
		/// </summary>
		/// <param name="a">First vector</param>
		/// <param name="b">Second vector</param>
		/// <returns>The distance between the vectors</returns>
		public static FP Distance(FPVector3 a, FPVector3 b)
		{
			a.X.RawValue = a.X.RawValue - b.X.RawValue;
			a.Y.RawValue = a.Y.RawValue - b.Y.RawValue;
			a.Z.RawValue = a.Z.RawValue - b.Z.RawValue;
			a.X.RawValue = FPMath.SqrtRaw((a.X.RawValue * a.X.RawValue + 32768 >> 16) + (a.Y.RawValue * a.Y.RawValue + 32768 >> 16) + (a.Z.RawValue * a.Z.RawValue + 32768 >> 16));
			return a.X;
		}

		/// <summary>
		/// Calculates the squared distance between two vectors
		/// </summary>
		/// <param name="a">First vector</param>
		/// <param name="b">Second vector</param>
		/// <returns>The squared distance between the vectors</returns>
		public static FP DistanceSquared(FPVector3 a, FPVector3 b)
		{
			a.X.RawValue = a.X.RawValue - b.X.RawValue;
			a.Y.RawValue = a.Y.RawValue - b.Y.RawValue;
			a.Z.RawValue = a.Z.RawValue - b.Z.RawValue;
			a.X.RawValue = (a.X.RawValue * a.X.RawValue + 32768 >> 16) + (a.Y.RawValue * a.Y.RawValue + 32768 >> 16) + (a.Z.RawValue * a.Z.RawValue + 32768 >> 16);
			return a.X;
		}

		/// <summary>
		/// The cross product of two vectors.
		/// </summary>
		/// <param name="a">The first vector.</param>
		/// <param name="b">The second vector.</param>
		/// <returns>The cross product of both vectors.</returns>
		public static FPVector3 Cross(FPVector3 a, FPVector3 b)
		{
			FPVector3 result = default(FPVector3);
			result.X.RawValue = (a.Y.RawValue * b.Z.RawValue + 32768 >> 16) - (a.Z.RawValue * b.Y.RawValue + 32768 >> 16);
			result.Y.RawValue = (a.Z.RawValue * b.X.RawValue + 32768 >> 16) - (a.X.RawValue * b.Z.RawValue + 32768 >> 16);
			result.Z.RawValue = (a.X.RawValue * b.Y.RawValue + 32768 >> 16) - (a.Y.RawValue * b.X.RawValue + 32768 >> 16);
			return result;
		}

		/// <summary>
		/// Calculates the dot product of two vectors.
		/// </summary>
		/// <param name="a">The first vector.</param>
		/// <param name="b">The second vector.</param>
		/// <returns>Returns the dot product of both vectors.</returns>
		public static FP Dot(FPVector3 a, FPVector3 b)
		{
			a.X.RawValue = (a.X.RawValue * b.X.RawValue + 32768 >> 16) + (a.Y.RawValue * b.Y.RawValue + 32768 >> 16) + (a.Z.RawValue * b.Z.RawValue + 32768 >> 16);
			return a.X;
		}

		/// <summary>
		/// Returns the signed angle in degrees between <paramref name="a" /> and <paramref name="b" /> when rotated around an <paramref name="axis" />.
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <param name="axis"></param>
		/// <returns></returns>
		public static FP SignedAngle(FPVector3 a, FPVector3 b, FPVector3 axis)
		{
			FP result = Angle(a, b);
			FPVector3 fPVector = default(FPVector3);
			fPVector.X.RawValue = (a.Y.RawValue * b.Z.RawValue + 32768 >> 16) - (a.Z.RawValue * b.Y.RawValue + 32768 >> 16);
			fPVector.Y.RawValue = (a.Z.RawValue * b.X.RawValue + 32768 >> 16) - (a.X.RawValue * b.Z.RawValue + 32768 >> 16);
			fPVector.Z.RawValue = (a.X.RawValue * b.Y.RawValue + 32768 >> 16) - (a.Y.RawValue * b.X.RawValue + 32768 >> 16);
			a.X.RawValue = (fPVector.X.RawValue * axis.X.RawValue + 32768 >> 16) + (fPVector.Y.RawValue * axis.Y.RawValue + 32768 >> 16) + (fPVector.Z.RawValue * axis.Z.RawValue + 32768 >> 16);
			if (a.X.RawValue < 0)
			{
				result.RawValue = -result.RawValue;
			}
			return result;
		}

		/// <summary>
		/// Returns the angle in degrees between <paramref name="a" /> and <paramref name="b" />.
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static FP Angle(FPVector3 a, FPVector3 b)
		{
			long num = (a.X.RawValue * a.X.RawValue + 32768 >> 16) + (a.Y.RawValue * a.Y.RawValue + 32768 >> 16) + (a.Z.RawValue * a.Z.RawValue + 32768 >> 16);
			if (num == 0L)
			{
				return default(FP);
			}
			num = 4294967296L / FPMath.SqrtRaw(num);
			a.X.RawValue = a.X.RawValue * num + 32768 >> 16;
			a.Y.RawValue = a.Y.RawValue * num + 32768 >> 16;
			a.Z.RawValue = a.Z.RawValue * num + 32768 >> 16;
			num = (b.X.RawValue * b.X.RawValue + 32768 >> 16) + (b.Y.RawValue * b.Y.RawValue + 32768 >> 16) + (b.Z.RawValue * b.Z.RawValue + 32768 >> 16);
			if (num == 0L)
			{
				return default(FP);
			}
			num = 4294967296L / FPMath.SqrtRaw(num);
			b.X.RawValue = b.X.RawValue * num + 32768 >> 16;
			b.Y.RawValue = b.Y.RawValue * num + 32768 >> 16;
			b.Z.RawValue = b.Z.RawValue * num + 32768 >> 16;
			num = (a.X.RawValue * b.X.RawValue + 32768 >> 16) + (a.Y.RawValue * b.Y.RawValue + 32768 >> 16) + (a.Z.RawValue * b.Z.RawValue + 32768 >> 16);
			if (num < -65534)
			{
				num = -65536L;
			}
			else if (num > 65534)
			{
				num = 65536L;
			}
			num = FPLut.acos_lut[num + 65536];
			a.X.RawValue = num * FP.Rad2Deg.RawValue + 32768 >> 16;
			return a.X;
		}

		/// <summary>
		/// Calculate a position between the points specified by <paramref name="from" /> and <paramref name="to" />, moving no farther than the distance specified by <paramref name="maxDelta" />.
		/// </summary>
		/// <param name="from"></param>
		/// <param name="to"></param>
		/// <param name="maxDelta"></param>
		/// <returns></returns>
		public static FPVector3 MoveTowards(FPVector3 from, FPVector3 to, FP maxDelta)
		{
			FPVector3 fPVector = default(FPVector3);
			fPVector.X.RawValue = to.X.RawValue - from.X.RawValue;
			fPVector.Y.RawValue = to.Y.RawValue - from.Y.RawValue;
			fPVector.Z.RawValue = to.Z.RawValue - from.Z.RawValue;
			long num = FPMath.SqrtRaw((fPVector.X.RawValue * fPVector.X.RawValue + 32768 >> 16) + (fPVector.Y.RawValue * fPVector.Y.RawValue + 32768 >> 16) + (fPVector.Z.RawValue * fPVector.Z.RawValue + 32768 >> 16));
			if (num <= maxDelta.RawValue || num == 0L)
			{
				return to;
			}
			from.X.RawValue += (fPVector.X.RawValue << 16) / num * maxDelta.RawValue + 32768 >> 16;
			from.Y.RawValue += (fPVector.Y.RawValue << 16) / num * maxDelta.RawValue + 32768 >> 16;
			from.Z.RawValue += (fPVector.Z.RawValue << 16) / num * maxDelta.RawValue + 32768 >> 16;
			return from;
		}

		/// <summary>
		/// Projects a vector onto another vector.
		/// </summary>
		/// <param name="vector"></param>
		/// <param name="normal"></param>
		/// <returns></returns>
		public static FPVector3 Project(FPVector3 vector, FPVector3 normal)
		{
			FP fP = Dot(normal, normal);
			if (fP < FP.Epsilon)
			{
				return Zero;
			}
			return normal * Dot(vector, normal) / fP;
		}

		/// <summary>
		/// Projects a vector onto a plane defined by a normal orthogonal to the plane.
		/// </summary>
		/// <param name="vector"></param>
		/// <param name="planeNormal"></param>
		/// <returns>The location of the vector on the plane. </returns>
		public static FPVector3 ProjectOnPlane(FPVector3 vector, FPVector3 planeNormal)
		{
			return vector - Project(vector, planeNormal);
		}

		/// <summary>
		/// Reflects a vector off the plane defined by a normal.
		/// </summary>
		/// <param name="vector"></param>
		/// <param name="normal"></param>
		/// <returns></returns>
		public static FPVector3 Reflect(FPVector3 vector, FPVector3 normal)
		{
			return -2 * Dot(normal, vector) * normal + vector;
		}

		/// <summary>
		/// Creates barycentric coordinates for a point inside a triangle. This method has precision issues due to multiple dot product in row this marked internal..
		/// </summary>
		/// <param name="p">Point of interest in triangle</param>
		/// <param name="p0">Vertex 1</param>
		/// <param name="p1">Vertex 2</param>
		/// <param name="p2">Vertex 3</param>
		/// <param name="u">Barycentric variable for p0</param>
		/// <param name="v">Barycentric variable for p1</param>
		/// <param name="w">Barycentric variable for p2</param>
		/// <returns><see langword="true" />, if point is inside the triangle. Out parameter are not set if the point is outside the triangle.</returns>
		internal static bool Barycentric(FPVector3 p, FPVector3 p0, FPVector3 p1, FPVector3 p2, out FP u, out FP v, out FP w)
		{
			v.RawValue = 0L;
			w.RawValue = 0L;
			u.RawValue = 0L;
			FPVector3 fPVector = default(FPVector3);
			FPVector3 fPVector2 = default(FPVector3);
			fPVector2.X.RawValue = p1.X.RawValue - p0.X.RawValue;
			fPVector2.Y.RawValue = p1.Y.RawValue - p0.Y.RawValue;
			fPVector2.Z.RawValue = p1.Z.RawValue - p0.Z.RawValue;
			FPVector3 fPVector3 = default(FPVector3);
			fPVector3.X.RawValue = p2.X.RawValue - p0.X.RawValue;
			fPVector3.Y.RawValue = p2.Y.RawValue - p0.Y.RawValue;
			fPVector3.Z.RawValue = p2.Z.RawValue - p0.Z.RawValue;
			fPVector.X.RawValue = p.X.RawValue - p0.X.RawValue;
			fPVector.Y.RawValue = p.Y.RawValue - p0.Y.RawValue;
			fPVector.Z.RawValue = p.Z.RawValue - p0.Z.RawValue;
			long num = (fPVector2.X.RawValue * fPVector2.X.RawValue + 32768 >> 16) + (fPVector2.Y.RawValue * fPVector2.Y.RawValue + 32768 >> 16) + (fPVector2.Z.RawValue * fPVector2.Z.RawValue + 32768 >> 16);
			long num2 = (fPVector2.X.RawValue * fPVector3.X.RawValue + 32768 >> 16) + (fPVector2.Y.RawValue * fPVector3.Y.RawValue + 32768 >> 16) + (fPVector2.Z.RawValue * fPVector3.Z.RawValue + 32768 >> 16);
			long num3 = (fPVector3.X.RawValue * fPVector3.X.RawValue + 32768 >> 16) + (fPVector3.Y.RawValue * fPVector3.Y.RawValue + 32768 >> 16) + (fPVector3.Z.RawValue * fPVector3.Z.RawValue + 32768 >> 16);
			long num4 = (fPVector.X.RawValue * fPVector2.X.RawValue + 32768 >> 16) + (fPVector.Y.RawValue * fPVector2.Y.RawValue + 32768 >> 16) + (fPVector.Z.RawValue * fPVector2.Z.RawValue + 32768 >> 16);
			long num5 = (fPVector.X.RawValue * fPVector3.X.RawValue + 32768 >> 16) + (fPVector.Y.RawValue * fPVector3.Y.RawValue + 32768 >> 16) + (fPVector.Z.RawValue * fPVector3.Z.RawValue + 32768 >> 16);
			long num6 = (num * num3 + 32768 >> 16) - (num2 * num2 + 32768 >> 16);
			if (num6 < 0)
			{
				return false;
			}
			float num7 = num2 * num5 - num3 * num4;
			float num8 = num2 * num4 - num * num5;
			if (num7 + num8 <= (float)num6 && num7 >= 0f)
			{
				_ = 0f;
			}
			v.RawValue = ((num3 * num4 + 32768 >> 16) - (num2 * num5 + 32768 >> 16) << 16) / num6;
			w.RawValue = ((num * num5 + 32768 >> 16) - (num2 * num4 + 32768 >> 16) << 16) / num6;
			u.RawValue = FP._1.RawValue - v.RawValue - w.RawValue;
			return true;
		}

		/// <summary>
		/// Returns <see langword="true" /> if two vectors are exactly equal.
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator ==(FPVector3 a, FPVector3 b)
		{
			if (a.X.RawValue == b.X.RawValue && a.Y.RawValue == b.Y.RawValue)
			{
				return a.Z.RawValue == b.Z.RawValue;
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
		public static bool operator !=(FPVector3 a, FPVector3 b)
		{
			if (a.X.RawValue == b.X.RawValue && a.Y.RawValue == b.Y.RawValue)
			{
				return a.Z.RawValue != b.Z.RawValue;
			}
			return true;
		}

		/// <summary>
		/// Negates each component of <paramref name="v" /> vector.
		/// </summary>
		/// <param name="v"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static FPVector3 operator -(FPVector3 v)
		{
			v.X.RawValue = -v.X.RawValue;
			v.Y.RawValue = -v.Y.RawValue;
			v.Z.RawValue = -v.Z.RawValue;
			return v;
		}

		/// <summary>
		/// Multiplies each component of <paramref name="v" /> times <paramref name="s" />.
		/// </summary>
		/// <param name="v"></param>
		/// <param name="s"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static FPVector3 operator *(FPVector3 v, FP s)
		{
			v.X.RawValue = v.X.RawValue * s.RawValue + 32768 >> 16;
			v.Y.RawValue = v.Y.RawValue * s.RawValue + 32768 >> 16;
			v.Z.RawValue = v.Z.RawValue * s.RawValue + 32768 >> 16;
			return v;
		}

		/// <summary>
		/// Multiplies each component of <paramref name="v" /> times <paramref name="s" />.
		/// </summary>
		/// <param name="v"></param>
		/// <param name="s"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static FPVector3 operator *(FP s, FPVector3 v)
		{
			v.X.RawValue = v.X.RawValue * s.RawValue + 32768 >> 16;
			v.Y.RawValue = v.Y.RawValue * s.RawValue + 32768 >> 16;
			v.Z.RawValue = v.Z.RawValue * s.RawValue + 32768 >> 16;
			return v;
		}

		/// <summary>
		/// Divides each component of <paramref name="v" /> by <paramref name="s" />.
		/// </summary>
		/// <param name="v"></param>
		/// <param name="s"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static FPVector3 operator /(FPVector3 v, FP s)
		{
			v.X.RawValue = (v.X.RawValue << 16) / s.RawValue;
			v.Y.RawValue = (v.Y.RawValue << 16) / s.RawValue;
			v.Z.RawValue = (v.Z.RawValue << 16) / s.RawValue;
			return v;
		}

		/// <summary>
		/// Divides each component of <paramref name="v" /> by <paramref name="s" />.
		/// </summary>
		/// <param name="v"></param>
		/// <param name="s"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static FPVector3 operator /(FPVector3 v, int s)
		{
			v.X.RawValue /= s;
			v.Y.RawValue /= s;
			v.Z.RawValue /= s;
			return v;
		}

		/// <summary>
		/// Subtracts <paramref name="b" /> from <paramref name="a" />
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static FPVector3 operator -(FPVector3 a, FPVector3 b)
		{
			a.X.RawValue = a.X.RawValue - b.X.RawValue;
			a.Y.RawValue = a.Y.RawValue - b.Y.RawValue;
			a.Z.RawValue = a.Z.RawValue - b.Z.RawValue;
			return a;
		}

		/// <summary>
		/// Adds two vectors.
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static FPVector3 operator +(FPVector3 a, FPVector3 b)
		{
			a.X.RawValue = a.X.RawValue + b.X.RawValue;
			a.Y.RawValue = a.Y.RawValue + b.Y.RawValue;
			a.Z.RawValue = a.Z.RawValue + b.Z.RawValue;
			return a;
		}
	}
}

