using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Photon.Deterministic
{
	/// <summary>
	/// Represents a two-dimensional vector with integer components.
	/// </summary>
	/// \ingroup MathApi
	[Serializable]
	[StructLayout(LayoutKind.Explicit)]
	public struct IntVector2 : IEquatable<IntVector2>
	{
		/// <summary>
		/// Represents an equality comparer for IntVector2 objects.
		/// </summary>
		public class EqualityComparer : IEqualityComparer<IntVector2>
		{
			/// <summary>
			/// The global equality comparer instance.
			/// </summary>
			public static readonly EqualityComparer Instance = new EqualityComparer();

			private EqualityComparer()
			{
			}

			/// <summary>
			/// Determines whether the current instance is equal to the specified object.
			/// </summary>
			public bool Equals(IntVector2 x, IntVector2 y)
			{
				return x == y;
			}

			/// <summary>
			/// Computes the hash code for the IntVector2.
			/// </summary>
			/// <returns>
			/// The computed hash code.
			/// </returns>
			public int GetHashCode(IntVector2 obj)
			{
				return obj.GetHashCode();
			}
		}

		/// <summary>
		/// The size of the struct in memory.
		/// </summary>
		public const int SIZE = 8;

		/// <summary>The X component of the vector.</summary>
		[FieldOffset(0)]
		public int X;

		/// <summary>The Y component of the vector.</summary>
		[FieldOffset(4)]
		public int Y;

		/// <summary>
		/// A vector with components (0,0);
		/// </summary>
		public static IntVector2 Zero => new IntVector2(0, 0);

		/// <summary>
		/// A vector with components (1,1);
		/// </summary>
		public static IntVector2 One => new IntVector2(1, 1);

		/// <summary>
		/// A vector with components (1,0);
		/// </summary>
		public static IntVector2 Right => new IntVector2(1, 0);

		/// <summary>
		/// A vector with components (-1,0);
		/// </summary>
		public static IntVector2 Left => new IntVector2(-1, 0);

		/// <summary>
		/// A vector with components (0,1);
		/// </summary>
		public static IntVector2 Up => new IntVector2(0, 1);

		/// <summary>
		/// A vector with components (0,-1);
		/// </summary>
		public static IntVector2 Down => new IntVector2(0, -1);

		/// <summary>
		/// A vector with components (int.MaxValue, int.MaxValue);
		/// </summary>
		public static IntVector2 MaxValue => new IntVector2(int.MaxValue, int.MaxValue);

		/// <summary>
		/// A vector with components (int.MinValue, int.MinValue);
		/// </summary>
		public static IntVector2 MinValue => new IntVector2(int.MinValue, int.MinValue);

		/// <summary>
		/// Returns vector (X, 0, Y).
		/// </summary>
		public readonly IntVector3 XOY
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				return new IntVector3(X, 0, Y);
			}
		}

		/// <summary>
		/// Returns vector (X, Y, 0).
		/// </summary>
		public readonly IntVector3 XYO
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				return new IntVector3(X, Y, 0);
			}
		}

		/// <summary>
		/// Returns vector (0, X, Y).
		/// </summary>
		public readonly IntVector3 OXY
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				return new IntVector3(0, X, Y);
			}
		}

		/// <summary>
		/// Gets the magnitude of the vector.
		/// </summary>
		public readonly FP Magnitude
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				return FPMath.Sqrt(X * X + Y * Y);
			}
		}

		/// <summary>
		/// Gets the squared magnitude of the vector.
		/// </summary>
		public readonly int SqrMagnitude
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				return X * X + Y * Y;
			}
		}

		/// <summary>
		/// Returns a new IntVector3 using the X, X and X components of this vector.
		/// </summary>
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

		/// <summary>
		/// Returns a new IntVector3 using the X, X and Y components of this vector.
		/// </summary>
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

		/// <summary>
		/// Returns a new IntVector3 using the X, Y and X components of this vector.
		/// </summary>
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

		/// <summary>
		/// Returns a new IntVector3 using the X, Y and Y components of this vector.
		/// </summary>
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

		/// <summary>
		/// Returns a new IntVector2 using the X and X components of this vector.
		/// </summary>
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

		/// <summary>
		/// Returns a new IntVector2 using the X and Y components of this vector.
		/// </summary>
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

		/// <summary>
		/// Returns a new IntVector3 using the Y, Y and Y components of this vector.
		/// </summary>
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

		/// <summary>
		/// Returns a new IntVector3 using the Y, Y and X components of this vector.
		/// </summary>
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

		/// <summary>
		/// Returns a new IntVector3 using the Y, X and Y components of this vector.
		/// </summary>
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

		/// <summary>
		/// Returns a new IntVector3 using the Y, X and X components of this vector.
		/// </summary>
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

		/// <summary>
		/// Returns a new IntVector2 using the Y and Y components of this vector.
		/// </summary>
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

		/// <summary>
		/// Returns a new IntVector2 using the Y and X components of this vector.
		/// </summary>
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

		/// <summary>
		/// Initializes a new instance of the IntVector2 struct.
		/// </summary>
		/// <param name="x">The x-coordinate of the vector.</param>
		/// <param name="y">The y-coordinate of the vector.</param>
		public IntVector2(int x, int y)
		{
			X = x;
			Y = y;
		}

		/// <summary>
		/// Returns a hash code for this instance.
		/// </summary>
		public override readonly int GetHashCode()
		{
			int num = 17;
			num = num * 31 + X.GetHashCode();
			return num * 31 + Y.GetHashCode();
		}

		/// <summary>
		/// Returns a string that represents the current IntVector2.
		/// </summary>
		/// <returns>A string that represents the current IntVector2.</returns>
		public override readonly string ToString()
		{
			return string.Format(CultureInfo.InvariantCulture, "({0}, {1})", X, Y);
		}

		/// <summary>
		/// Serializes or deserializes a IntVector2 struct using the provided IDeterministicFrameSerializer object.
		/// </summary>
		/// <param name="ptr">A pointer to the IntVector2 struct to serialize or deserialize.</param>
		/// <param name="serializer">The IDeterministicFrameSerializer object used for serialization or deserialization.</param>
		public unsafe static void Serialize(void* ptr, IDeterministicFrameSerializer serializer)
		{
			if (serializer.Writing)
			{
				serializer.Stream.WriteInt(((IntVector2*)ptr)->X);
				serializer.Stream.WriteInt(((IntVector2*)ptr)->Y);
			}
			else
			{
				((IntVector2*)ptr)->X = serializer.Stream.ReadInt();
				((IntVector2*)ptr)->Y = serializer.Stream.ReadInt();
			}
		}

		/// <summary>
		/// Clamps a IntVector2 value between a minimum and maximum value.
		/// </summary>
		/// <param name="value">The IntVector2 value to clamp</param>
		/// <param name="min">The minimum IntVector2 value to clamp to</param>
		/// <param name="max">The maximum IntVector2 value to clamp to</param>
		/// <returns>The clamped IntVector2 value</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static IntVector2 Clamp(IntVector2 value, IntVector2 min, IntVector2 max)
		{
			return new IntVector2(FPMath.Clamp(value.X, min.X, max.X), FPMath.Clamp(value.Y, min.Y, max.Y));
		}

		/// <summary>
		/// Calculates the distance between two IntVector2 points.
		/// </summary>
		/// <param name="a">The first IntVector2 point</param>
		/// <param name="b">The second IntVector2 point</param>
		/// <returns>The distance between the two IntVector2 points</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static FP Distance(IntVector2 a, IntVector2 b)
		{
			return (a - b).Magnitude;
		}

		/// <summary>
		/// Returns a new IntVector2 with the largest components of the input vectors.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static IntVector2 Max(IntVector2 a, IntVector2 b)
		{
			return new IntVector2(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y));
		}

		/// <summary>
		/// Returns a new IntVector2 with the smallest components of the input vectors.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static IntVector2 Min(IntVector2 a, IntVector2 b)
		{
			return new IntVector2(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y));
		}

		/// <summary>
		/// Rounds a FPVector2 to the nearest whole numbers, and returns a new IntVector2.
		/// </summary>
		/// <param name="v">The FPVector2 to round.</param>
		/// <returns>A new IntVector2 with the rounded components of the input vector.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static IntVector2 RoundToInt(FPVector2 v)
		{
			return new IntVector2(FPMath.RoundToInt(v.X), FPMath.RoundToInt(v.Y));
		}

		/// <summary>
		/// Returns the largest integer less than or equal to the specified floating-point number.
		/// </summary>
		/// <param name="v">The floating-point number.</param>
		/// <returns>The largest integer less than or equal to <paramref name="v" />.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static IntVector2 FloorToInt(FPVector2 v)
		{
			return new IntVector2(FPMath.FloorToInt(v.X), FPMath.FloorToInt(v.Y));
		}

		/// <summary>
		/// Returns a new IntVector2 with the smallest integer greater than or equal to the components of the given FPVector2.
		/// </summary>
		/// <param name="v">The FPVector2 to ceil.</param>
		/// <returns>A new IntVector2 with the components ceil'd.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static IntVector2 CeilToInt(FPVector2 v)
		{
			return new IntVector2(FPMath.CeilToInt(v.X), FPMath.CeilToInt(v.Y));
		}

		/// <summary>
		/// Implicitly converts a IntVector2 instance to an FPVector2 instance.
		/// This conversion creates a new FPVector2 instance with the X and Y coordinates
		/// from the input IntVector2 instance.
		/// </summary>
		/// <param name="v">The IntVector2 instance to convert.</param>
		/// <returns>A new FPVector2 instance with the X and Y coordinates from the input IntVector2 instance.</returns>
		public static implicit operator FPVector2(IntVector2 v)
		{
			return new FPVector2(v.X, v.Y);
		}

		/// <summary>
		/// Converts a FPVector2 instance to a IntVector2 instance.
		/// </summary>
		public static explicit operator IntVector2(FPVector2 v)
		{
			return new IntVector2(v.X.AsInt, v.Y.AsInt);
		}

		/// <summary>
		/// Adds two IntVector2 instances.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static IntVector2 operator +(IntVector2 a, IntVector2 b)
		{
			return new IntVector2(a.X + b.X, a.Y + b.Y);
		}

		/// <summary>
		/// Subtracts the second IntVector2 from the first.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static IntVector2 operator -(IntVector2 a, IntVector2 b)
		{
			return new IntVector2(a.X - b.X, a.Y - b.Y);
		}

		/// <summary>
		/// Multiplies a IntVector2 by an integer scalar.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static IntVector2 operator *(IntVector2 a, int d)
		{
			return new IntVector2(a.X * d, a.Y * d);
		}

		/// <summary>
		/// Multiplies a IntVector2 by an integer scalar.
		/// </summary>
		/// <param name="d"></param>
		/// <param name="a"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static IntVector2 operator *(int d, IntVector2 a)
		{
			return new IntVector2(a.X * d, a.Y * d);
		}

		/// <summary>
		/// Divides a IntVector2 by an integer scalar.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static IntVector2 operator /(IntVector2 a, int d)
		{
			return new IntVector2(a.X / d, a.Y / d);
		}

		/// <summary>
		/// Compares two IntVector2 instances for equality.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator ==(IntVector2 lhs, IntVector2 rhs)
		{
			if (lhs.X == rhs.X)
			{
				return lhs.Y == rhs.Y;
			}
			return false;
		}

		/// <summary>
		/// Compares two IntVector2 instances for inequality.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator !=(IntVector2 lhs, IntVector2 rhs)
		{
			return !(lhs == rhs);
		}

		/// <summary>
		/// Negates an IntVector2.
		/// </summary>
		/// <param name="a"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static IntVector2 operator -(IntVector2 a)
		{
			return new IntVector2(-a.X, -a.Y);
		}

		/// <summary>
		/// Determines whether the specified IntVector2 is equal to the current IntVector2.
		/// </summary>
		/// <param name="other">The IntVector2 to compare with the current IntVector2.</param>
		/// <returns>true if the specified IntVector2 is equal to the current IntVector2; otherwise, false.</returns>
		public readonly bool Equals(IntVector2 other)
		{
			if (X == other.X)
			{
				return Y == other.Y;
			}
			return false;
		}

		/// <summary>
		/// Determines whether the specified object is equal to the current IntVector2.
		/// </summary>
		/// <param name="obj">The object to compare with the current IntVector2.</param>
		/// <returns>true if the specified object is equal to the current IntVector2; otherwise, false.</returns>
		public override readonly bool Equals(object obj)
		{
			if (obj is IntVector2 other)
			{
				return Equals(other);
			}
			return false;
		}
	}
}

