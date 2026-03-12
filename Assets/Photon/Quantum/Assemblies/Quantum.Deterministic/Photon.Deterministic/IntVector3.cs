using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Photon.Deterministic
{
	/// <summary>
	/// Represents a three-dimensional vector with integer components.
	/// </summary>
	/// \ingroup MathApi
	[Serializable]
	[StructLayout(LayoutKind.Explicit)]
	public struct IntVector3 : IEquatable<IntVector3>
	{
		/// <summary>
		/// Represents an equality comparer for IntVector3 objects.
		/// </summary>
		public class EqualityComparer : IEqualityComparer<IntVector3>
		{
			/// <summary>
			/// The global equality comparer instance.
			/// </summary>
			public static readonly EqualityComparer Instance = new EqualityComparer();

			private EqualityComparer()
			{
			}

			public bool Equals(IntVector3 x, IntVector3 y)
			{
				return x == y;
			}

			public int GetHashCode(IntVector3 obj)
			{
				return obj.GetHashCode();
			}
		}

		/// <summary>
		/// The size of the struct in memory.
		/// </summary>
		public const int SIZE = 12;

		/// <summary>
		/// The X component of the vector.
		/// </summary>
		[FieldOffset(0)]
		public int X;

		/// <summary>
		/// The Y component of the vector.
		/// </summary>
		[FieldOffset(4)]
		public int Y;

		/// <summary>
		/// The Z component of the vector.
		/// </summary>
		[FieldOffset(8)]
		public int Z;

		/// <summary>
		/// A vector with the components (int.MaxValue, int.MaxValue, int.MaxValue).
		/// </summary>
		public static IntVector3 MaxValue => new IntVector3(int.MaxValue, int.MaxValue, int.MaxValue);

		/// <summary>
		/// A vector with the components (int.MinValue, int.MinValue, int.MinValue).
		/// </summary>
		public static IntVector3 MinValue => new IntVector3(int.MinValue, int.MinValue, int.MinValue);

		/// <summary>
		/// Represents an upward vector with x = 0, y = 1, and z = 0.
		/// </summary>
		public static IntVector3 Up => new IntVector3(0, 1, 0);

		/// <summary>
		/// Represents the downward direction with coordinates (0, -1, 0).
		/// </summary>
		public static IntVector3 Down => new IntVector3(0, -1, 0);

		/// <summary>
		/// Represents a 3-dimensional vector with integer components.
		/// </summary>
		/// <remarks>
		/// This struct is part of the <see cref="N:Photon.Deterministic" /> namespace.
		/// </remarks>
		public static IntVector3 Left => new IntVector3(-1, 0, 0);

		/// <summary>
		/// The right direction vector (1, 0, 0).
		/// </summary>
		/// <value>The right direction vector.</value>
		public static IntVector3 Right => new IntVector3(1, 0, 0);

		/// <summary>
		/// The vector representing a coordinate of (1, 1, 1).
		/// </summary>
		public static IntVector3 One => new IntVector3(1, 1, 1);

		/// <summary>
		/// Represents a zero vector with all components set to 0.
		/// </summary>
		public static IntVector3 Zero => new IntVector3(0, 0, 0);

		/// <summary>
		/// Calculates the magnitude (length) of the IntVector3.
		/// </summary>
		/// <value>
		/// The magnitude of the IntVector3.
		/// </value>
		public readonly FP Magnitude
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				return FPMath.Sqrt(X * X + Y * Y + Z * Z);
			}
		}

		/// <summary>
		/// Gets the square of the magnitude of the vector.
		/// </summary>
		/// <value>The square of the magnitude of the vector.</value>
		public readonly int SqrMagnitude
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				return X * X + Y * Y + Z * Z;
			}
		}

		public readonly IntVector3 XXX
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				IntVector3 result = default(IntVector3);
				result.X = X;
				result.Y = X;
				result.Z = X;
				return result;
			}
		}

		public readonly IntVector3 XXY
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				IntVector3 result = default(IntVector3);
				result.X = X;
				result.Y = X;
				result.Z = Y;
				return result;
			}
		}

		public readonly IntVector3 XXZ
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				IntVector3 result = default(IntVector3);
				result.X = X;
				result.Y = X;
				result.Z = Z;
				return result;
			}
		}

		public readonly IntVector3 XYX
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				IntVector3 result = default(IntVector3);
				result.X = X;
				result.Y = Y;
				result.Z = X;
				return result;
			}
		}

		public readonly IntVector3 XYY
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				IntVector3 result = default(IntVector3);
				result.X = X;
				result.Y = Y;
				result.Z = Y;
				return result;
			}
		}

		public readonly IntVector3 XYZ
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				IntVector3 result = default(IntVector3);
				result.X = X;
				result.Y = Y;
				result.Z = Z;
				return result;
			}
		}

		public readonly IntVector3 XZX
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				IntVector3 result = default(IntVector3);
				result.X = X;
				result.Y = Z;
				result.Z = X;
				return result;
			}
		}

		public readonly IntVector3 XZY
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				IntVector3 result = default(IntVector3);
				result.X = X;
				result.Y = Z;
				result.Z = Y;
				return result;
			}
		}

		public readonly IntVector3 XZZ
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				IntVector3 result = default(IntVector3);
				result.X = X;
				result.Y = Z;
				result.Z = Z;
				return result;
			}
		}

		public readonly IntVector2 XX
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				IntVector2 result = default(IntVector2);
				result.X = X;
				result.Y = X;
				return result;
			}
		}

		public readonly IntVector2 XY
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				IntVector2 result = default(IntVector2);
				result.X = X;
				result.Y = Y;
				return result;
			}
		}

		public readonly IntVector2 XZ
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				IntVector2 result = default(IntVector2);
				result.X = X;
				result.Y = Z;
				return result;
			}
		}

		public readonly IntVector3 YYY
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				IntVector3 result = default(IntVector3);
				result.X = Y;
				result.Y = Y;
				result.Z = Y;
				return result;
			}
		}

		public readonly IntVector3 YYZ
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				IntVector3 result = default(IntVector3);
				result.X = Y;
				result.Y = Y;
				result.Z = Z;
				return result;
			}
		}

		public readonly IntVector3 YYX
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				IntVector3 result = default(IntVector3);
				result.X = Y;
				result.Y = Y;
				result.Z = X;
				return result;
			}
		}

		public readonly IntVector3 YZY
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				IntVector3 result = default(IntVector3);
				result.X = Y;
				result.Y = Z;
				result.Z = Y;
				return result;
			}
		}

		public readonly IntVector3 YZZ
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				IntVector3 result = default(IntVector3);
				result.X = Y;
				result.Y = Z;
				result.Z = Z;
				return result;
			}
		}

		public readonly IntVector3 YZX
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				IntVector3 result = default(IntVector3);
				result.X = Y;
				result.Y = Z;
				result.Z = X;
				return result;
			}
		}

		public readonly IntVector3 YXY
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				IntVector3 result = default(IntVector3);
				result.X = Y;
				result.Y = X;
				result.Z = Y;
				return result;
			}
		}

		public readonly IntVector3 YXZ
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				IntVector3 result = default(IntVector3);
				result.X = Y;
				result.Y = X;
				result.Z = Z;
				return result;
			}
		}

		public readonly IntVector3 YXX
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				IntVector3 result = default(IntVector3);
				result.X = Y;
				result.Y = X;
				result.Z = X;
				return result;
			}
		}

		public readonly IntVector2 YY
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				IntVector2 result = default(IntVector2);
				result.X = Y;
				result.Y = Y;
				return result;
			}
		}

		public readonly IntVector2 YZ
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				IntVector2 result = default(IntVector2);
				result.X = Y;
				result.Y = Z;
				return result;
			}
		}

		public readonly IntVector2 YX
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				IntVector2 result = default(IntVector2);
				result.X = Y;
				result.Y = X;
				return result;
			}
		}

		public readonly IntVector3 ZZZ
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				IntVector3 result = default(IntVector3);
				result.X = Z;
				result.Y = Z;
				result.Z = Z;
				return result;
			}
		}

		public readonly IntVector3 ZZX
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				IntVector3 result = default(IntVector3);
				result.X = Z;
				result.Y = Z;
				result.Z = X;
				return result;
			}
		}

		public readonly IntVector3 ZZY
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				IntVector3 result = default(IntVector3);
				result.X = Z;
				result.Y = Z;
				result.Z = Y;
				return result;
			}
		}

		public readonly IntVector3 ZXZ
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				IntVector3 result = default(IntVector3);
				result.X = Z;
				result.Y = X;
				result.Z = Z;
				return result;
			}
		}

		public readonly IntVector3 ZXX
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				IntVector3 result = default(IntVector3);
				result.X = Z;
				result.Y = X;
				result.Z = X;
				return result;
			}
		}

		public readonly IntVector3 ZXY
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				IntVector3 result = default(IntVector3);
				result.X = Z;
				result.Y = X;
				result.Z = Y;
				return result;
			}
		}

		public readonly IntVector3 ZYZ
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				IntVector3 result = default(IntVector3);
				result.X = Z;
				result.Y = Y;
				result.Z = Z;
				return result;
			}
		}

		public readonly IntVector3 ZYX
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				IntVector3 result = default(IntVector3);
				result.X = Z;
				result.Y = Y;
				result.Z = X;
				return result;
			}
		}

		public readonly IntVector3 ZYY
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				IntVector3 result = default(IntVector3);
				result.X = Z;
				result.Y = Y;
				result.Z = Y;
				return result;
			}
		}

		public readonly IntVector2 ZZ
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				IntVector2 result = default(IntVector2);
				result.X = Z;
				result.Y = Z;
				return result;
			}
		}

		public readonly IntVector2 ZX
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				IntVector2 result = default(IntVector2);
				result.X = Z;
				result.Y = X;
				return result;
			}
		}

		public readonly IntVector2 ZY
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				IntVector2 result = default(IntVector2);
				result.X = Z;
				result.Y = Y;
				return result;
			}
		}

		/// <summary>
		/// Constructs a new IntVector3 with the given components.
		/// </summary>
		public IntVector3(int x, int y, int z)
		{
			X = x;
			Y = y;
			Z = z;
		}

		/// <summary>
		/// Returns a hash code for the vector.
		/// </summary>
		public override readonly int GetHashCode()
		{
			int num = 17;
			num = num * 31 + X.GetHashCode();
			num = num * 31 + Y.GetHashCode();
			return num * 31 + Z.GetHashCode();
		}

		/// <summary>
		/// Returns a string representation of the IntVector3.
		/// </summary>
		/// <returns>A string representation of the IntVector3.</returns>
		public override readonly string ToString()
		{
			return string.Format(CultureInfo.InvariantCulture, "({0}, {1}, {2})", X, Y, Z);
		}

		/// <summary>
		/// Serializes a IntVector3 to a stream using an IDeterministicFrameSerializer.
		/// If the serializer is in writing mode, the method writes the X, Y, and Z components of the vector to the stream.
		/// If the serializer is in reading mode, the method reads the X, Y, and Z components from the stream and assigns them to the vector.
		/// </summary>
		/// <param name="ptr">A pointer to the IntVector3 that is being serialized.</param>
		/// <param name="serializer">The IDeterministicFrameSerializer used for serialization.</param>
		public unsafe static void Serialize(void* ptr, IDeterministicFrameSerializer serializer)
		{
			if (serializer.Writing)
			{
				serializer.Stream.WriteInt(((IntVector3*)ptr)->X);
				serializer.Stream.WriteInt(((IntVector3*)ptr)->Y);
				serializer.Stream.WriteInt(((IntVector3*)ptr)->Z);
			}
			else
			{
				((IntVector3*)ptr)->X = serializer.Stream.ReadInt();
				((IntVector3*)ptr)->Y = serializer.Stream.ReadInt();
				((IntVector3*)ptr)->Z = serializer.Stream.ReadInt();
			}
		}

		/// <summary>
		/// Calculates the distance between two IntVector3 points.
		/// </summary>
		/// <param name="a">The first IntVector3 point.</param>
		/// <param name="b">The second IntVector3 point.</param>
		/// <returns>The distance between the two IntVector3 points.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static FP Distance(IntVector3 a, IntVector3 b)
		{
			return (a - b).Magnitude;
		}

		/// <summary>
		/// Clamps a IntVector3 value between a minimum and maximum IntVector3 value.
		/// </summary>
		/// <param name="value">The IntVector3 value to clamp.</param>
		/// <param name="min">The minimum IntVector3 value.</param>
		/// <param name="max">The maximum IntVector3 value.</param>
		/// <returns>The clamped IntVector3 value.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static IntVector3 Clamp(IntVector3 value, IntVector3 min, IntVector3 max)
		{
			return new IntVector3(FPMath.Clamp(value.X, min.X, max.X), FPMath.Clamp(value.Y, min.Y, max.Y), FPMath.Clamp(value.Z, min.Z, max.Z));
		}

		/// <summary>
		/// Returns a new IntVector3 with the smallest components of the input vectors.
		/// </summary>
		/// <param name="a">The first IntVector3.</param>
		/// <param name="b">The second IntVector3.</param>
		/// <returns>A new IntVector3 with the smallest components of the input vectors.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static IntVector3 Min(IntVector3 a, IntVector3 b)
		{
			return new IntVector3(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y), Math.Min(a.Z, b.Z));
		}

		/// <summary>
		/// Returns a new IntVector3 with the largest components of the input vectors.
		/// </summary>
		/// <param name="a">The first IntVector3.</param>
		/// <param name="b">The second IntVector3.</param>
		/// <returns>A new IntVector3 with the largest components of the input vectors.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static IntVector3 Max(IntVector3 a, IntVector3 b)
		{
			return new IntVector3(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y), Math.Max(a.Z, b.Z));
		}

		/// <summary>
		/// Rounds the components of the given FPVector3 to the nearest integer values.
		/// </summary>
		/// <param name="v">The vector to round.</param>
		/// <returns>A new IntVector3 with the rounded integer values.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static IntVector3 RoundToInt(FPVector3 v)
		{
			return new IntVector3(FPMath.RoundToInt(v.X), FPMath.RoundToInt(v.Y), FPMath.RoundToInt(v.Z));
		}

		/// <summary>
		/// Returns the largest integer less than or equal to the specified floating-point number.
		/// </summary>
		/// <param name="v">The number to round down.</param>
		/// <returns>The largest integer less than or equal to the specified number.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static IntVector3 FloorToInt(FPVector3 v)
		{
			return new IntVector3(FPMath.FloorToInt(v.X), FPMath.FloorToInt(v.Y), FPMath.FloorToInt(v.Z));
		}

		/// <summary>
		/// Returns a new IntVector3 with the ceiling of the components of the input vector.
		/// </summary>
		/// <param name="v">The input vector.</param>
		/// <returns>A new IntVector3 with the ceiling of the components.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static IntVector3 CeilToInt(FPVector3 v)
		{
			return new IntVector3(FPMath.CeilToInt(v.X), FPMath.CeilToInt(v.Y), FPMath.CeilToInt(v.Z));
		}

		/// <summary>
		/// Converts a IntVector3 object to FPVector3 object.
		/// </summary>
		/// <param name="v">The IntVector3 object to convert.</param>
		/// <returns>A new FPVector3 object created using the values of the IntVector3 object.</returns>
		public static implicit operator FPVector3(IntVector3 v)
		{
			return new FPVector3(v.X, v.Y, v.Z);
		}

		/// <summary>
		/// Converts a FPVector3 object to a IntVector3 object.
		/// </summary>
		public static explicit operator IntVector3(FPVector3 v)
		{
			return new IntVector3(v.X.AsInt, v.Y.AsInt, v.Z.AsInt);
		}

		/// <summary>
		/// Returns <see langword="true" /> if two vectors are exactly equal.
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator ==(IntVector3 a, IntVector3 b)
		{
			if (a.X == b.X && a.Y == b.Y)
			{
				return a.Z == b.Z;
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
		public static bool operator !=(IntVector3 a, IntVector3 b)
		{
			if (a.X == b.X && a.Y == b.Y)
			{
				return a.Z != b.Z;
			}
			return true;
		}

		/// <summary>
		/// Negates each component of <paramref name="v" /> vector.
		/// </summary>
		/// <param name="v"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static IntVector3 operator -(IntVector3 v)
		{
			v.X = -v.X;
			v.Y = -v.Y;
			v.Z = -v.Z;
			return v;
		}

		/// <summary>
		/// Multiplies each component of <paramref name="v" /> times <paramref name="s" />.
		/// </summary>
		/// <param name="v"></param>
		/// <param name="s"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static IntVector3 operator *(IntVector3 v, int s)
		{
			v.X *= s;
			v.Y *= s;
			v.Z *= s;
			return v;
		}

		/// <summary>
		/// Multiplies each component of <paramref name="v" /> times <paramref name="s" />.
		/// </summary>
		/// <param name="v"></param>
		/// <param name="s"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static IntVector3 operator *(int s, IntVector3 v)
		{
			v.X *= s;
			v.Y *= s;
			v.Z *= s;
			return v;
		}

		/// <summary>
		/// Divides each component of <paramref name="v" /> by <paramref name="s" />.
		/// </summary>
		/// <param name="v"></param>
		/// <param name="s"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static IntVector3 operator /(IntVector3 v, int s)
		{
			v.X /= s;
			v.Y /= s;
			v.Z /= s;
			return v;
		}

		/// <summary>
		/// Subtracts <paramref name="b" /> from <paramref name="a" />
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static IntVector3 operator -(IntVector3 a, IntVector3 b)
		{
			return new IntVector3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
		}

		/// <summary>
		/// Adds two vectors.
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static IntVector3 operator +(IntVector3 a, IntVector3 b)
		{
			return new IntVector3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
		}

		/// <summary>
		/// Returns true if the vector is exactly equal to another vector.
		/// </summary>
		public readonly bool Equals(IntVector3 other)
		{
			if (X == other.X && Y == other.Y)
			{
				return Z == other.Z;
			}
			return false;
		}

		/// <inheritdoc cref="M:Photon.Deterministic.IntVector3.Equals(Photon.Deterministic.IntVector3)" />
		public override readonly bool Equals(object obj)
		{
			if (obj is IntVector3 other)
			{
				return Equals(other);
			}
			return false;
		}
	}
}

