using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Photon.Deterministic
{
	/// <summary>
	/// A Quaternion representing an orientation.
	/// </summary>
	/// \ingroup MathAPI
	[Serializable]
	[StructLayout(LayoutKind.Explicit)]
	public struct FPQuaternion
	{
		/// <summary>
		/// The size of the struct in-memory inside the Frame data-buffers or stack (when passed as value parameter).
		/// Not related to the snapshot payload this occupies, which is bit-packed and compressed.
		/// </summary>
		public const int SIZE = 32;

		/// <summary>The X component of the quaternion.</summary>
		[FieldOffset(0)]
		public FP X;

		/// <summary>The Y component of the quaternion.</summary>
		[FieldOffset(8)]
		public FP Y;

		/// <summary>The Z component of the quaternion.</summary>
		[FieldOffset(16)]
		public FP Z;

		/// <summary>The W component of the quaternion.</summary>
		[FieldOffset(24)]
		public FP W;

		/// <summary>
		/// Quaternion corresponding to "no rotation".
		/// </summary>
		public static FPQuaternion Identity => new FPQuaternion
		{
			W = 
			{
				RawValue = 65536L
			}
		};

		/// <summary>
		/// Returns this quaternion with magnitude of 1. Most API functions expect and return normalized quaternions,
		/// so unless components get set manually, there should not be a need to normalize quaternions
		/// </summary>
		/// <seealso cref="M:Photon.Deterministic.FPQuaternion.Normalize(Photon.Deterministic.FPQuaternion)" />
		public readonly FPQuaternion Normalized => Normalize(this);

		/// <summary>
		/// Creates this quaternion's inverse. If this quaternion is normalized, use <see cref="P:Photon.Deterministic.FPQuaternion.Conjugated" /> instead.
		/// </summary>
		/// <seealso cref="M:Photon.Deterministic.FPQuaternion.Inverse(Photon.Deterministic.FPQuaternion)" />
		public readonly FPQuaternion Inverted => Inverse(this);

		/// <summary>
		/// Creates this quaternion's conjugate. For normalized quaternions this property represents inverse rotation
		/// and should be used instead of <see cref="P:Photon.Deterministic.FPQuaternion.Inverted" />
		/// </summary>
		/// <seealso cref="M:Photon.Deterministic.FPQuaternion.Conjugate(Photon.Deterministic.FPQuaternion)" />
		public readonly FPQuaternion Conjugated => Conjugate(this);

		private readonly long MagnitudeSqrRaw => (X.RawValue * X.RawValue + 32768 >> 16) + (Y.RawValue * Y.RawValue + 32768 >> 16) + (Z.RawValue * Z.RawValue + 32768 >> 16) + (W.RawValue * W.RawValue + 32768 >> 16);

		/// <summary>
		/// Returns square of this quaternion's magnitude.
		/// </summary>
		public readonly FP MagnitudeSqr => FP.FromRaw(MagnitudeSqrRaw);

		/// <summary>
		/// Return this quaternion's magnitude.
		/// </summary>
		public readonly FP Magnitude => FP.FromRaw(FPMath.SqrtRaw(MagnitudeSqrRaw));

		/// <summary>
		/// Returns one of possible Euler angles representation, where rotations are performed around the Z axis, the X axis, and the Y axis, in that order. 
		/// </summary>
		public readonly FPVector3 AsEuler => ToEulerZXY(this);

		/// <summary>
		/// Serializes the given instance of FPQuaternion using the provided serializer.
		/// </summary>
		/// <param name="ptr">Pointer to the instance of FPQuaternion.</param>
		/// <param name="serializer">The serializer to use.</param>
		public unsafe static void Serialize(void* ptr, IDeterministicFrameSerializer serializer)
		{
			FP.Serialize(&((FPQuaternion*)ptr)->X, serializer);
			FP.Serialize(&((FPQuaternion*)ptr)->Y, serializer);
			FP.Serialize(&((FPQuaternion*)ptr)->Z, serializer);
			FP.Serialize(&((FPQuaternion*)ptr)->W, serializer);
		}

		/// <summary>
		/// Creates a new instance of FPQuaternion
		/// </summary>
		/// <param name="x">X component.</param>
		/// <param name="y">Y component.</param>
		/// <param name="z">Z component.</param>
		/// <param name="w">W component.</param>
		public FPQuaternion(FP x, FP y, FP z, FP w)
		{
			X = x;
			Y = y;
			Z = z;
			W = w;
		}

		/// <summary>
		/// Returns a string representation of the FPQuaternion in the format (X, Y, Z, W).
		/// </summary>
		/// <returns>A string representation of the FPQuaternion.</returns>
		public override readonly string ToString()
		{
			return string.Format(CultureInfo.InvariantCulture, "({0:f1}, {1:f1}, {2:f1}, {3:f1})", X.AsFloat, Y.AsFloat, Z.AsFloat, W.AsFloat);
		}

		/// <summary>
		/// Returns a hash code for the current FPQuaternion object.
		/// </summary>
		/// <returns>A hash code for the current FPQuaternion object.</returns>
		public override readonly int GetHashCode()
		{
			int num = 17;
			num = num * 31 + X.GetHashCode();
			num = num * 31 + Y.GetHashCode();
			num = num * 31 + Z.GetHashCode();
			return num * 31 + W.GetHashCode();
		}

		/// <summary>
		/// Creates product of two quaternions. Can be used to combine two rotations. Just like
		/// in the case of <see cref="T:Photon.Deterministic.FPMatrix4x4" /> the righmost operand gets applied first.
		/// This method computes the equivalent to the following pseduo-code:
		/// <code>
		/// FPQuaternion result;
		/// result.x = (left.w * right.x) + (left.x * right.w) + (left.y * right.z) - (left.z * right.y);
		/// result.y = (left.w * right.y) - (left.x * right.z) + (left.y * right.w) + (left.z * right.x);
		/// result.z = (left.w * right.z) + (left.x * right.y) - (left.y * right.x) + (left.z * right.w);
		/// result.w = (left.w * right.w) - (left.x * right.x) - (left.y * right.y) - (left.z * right.z);
		/// return result;
		/// </code>
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static FPQuaternion Product(FPQuaternion left, FPQuaternion right)
		{
			long rawValue = left.X.RawValue;
			long rawValue2 = left.Y.RawValue;
			long rawValue3 = left.Z.RawValue;
			long rawValue4 = left.W.RawValue;
			long rawValue5 = right.X.RawValue;
			long rawValue6 = right.Y.RawValue;
			long rawValue7 = right.Z.RawValue;
			long rawValue8 = right.W.RawValue;
			FPQuaternion result = default(FPQuaternion);
			result.X.RawValue = (rawValue4 * rawValue5 + 32768 >> 16) + (rawValue * rawValue8 + 32768 >> 16) + (rawValue2 * rawValue7 + 32768 >> 16) - (rawValue3 * rawValue6 + 32768 >> 16);
			result.Y.RawValue = (rawValue4 * rawValue6 + 32768 >> 16) + (rawValue2 * rawValue8 + 32768 >> 16) + (rawValue3 * rawValue5 + 32768 >> 16) - (rawValue * rawValue7 + 32768 >> 16);
			result.Z.RawValue = (rawValue4 * rawValue7 + 32768 >> 16) + (rawValue3 * rawValue8 + 32768 >> 16) + (rawValue * rawValue6 + 32768 >> 16) - (rawValue2 * rawValue5 + 32768 >> 16);
			result.W.RawValue = (rawValue4 * rawValue8 + 32768 >> 16) - (rawValue * rawValue5 + 32768 >> 16) - (rawValue2 * rawValue6 + 32768 >> 16) - (rawValue3 * rawValue7 + 32768 >> 16);
			return result;
		}

		/// <summary>
		/// Returns conjugate quaternion. This method computes the equivalent to the following pseduo-code:
		/// <code>
		/// return new FPQuaternion(-value.X, -value.Y, -value.Z, value.W);
		/// </code>
		/// Conjugate can be used instead of an inverse quaterion if <paramref name="value" /> is normalized.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static FPQuaternion Conjugate(FPQuaternion value)
		{
			value.X.RawValue = -value.X.RawValue;
			value.Y.RawValue = -value.Y.RawValue;
			value.Z.RawValue = -value.Z.RawValue;
			return value;
		}

		/// <summary>
		/// Checks if the quaternion is the identity quaternion
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsIdentity(FPQuaternion value)
		{
			if (value.X.RawValue == 0L && value.Y.RawValue == 0L && value.Z.RawValue == 0L)
			{
				return value.W.RawValue == 65536;
			}
			return false;
		}

		/// <summary>
		/// Checks if the quaternion is the invalid zero quaternion
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsZero(FPQuaternion value)
		{
			if (value.X.RawValue == 0L && value.Y.RawValue == 0L && value.Z.RawValue == 0L)
			{
				return value.W.RawValue == 0;
			}
			return false;
		}

		/// <summary>
		/// Returns the dot product between two rotations. This method computes the equivalent to the following pseduo-code:
		/// <code>
		/// return a.X * b.X + a.Y * b.Y + a.Z * b.Z + a.W * b.W;
		/// </code>
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static FP Dot(FPQuaternion a, FPQuaternion b)
		{
			FP result = default(FP);
			result.RawValue = (a.W.RawValue * b.W.RawValue + 32768 >> 16) + (a.X.RawValue * b.X.RawValue + 32768 >> 16) + (a.Y.RawValue * b.Y.RawValue + 32768 >> 16) + (a.Z.RawValue * b.Z.RawValue + 32768 >> 16);
			return result;
		}

		/// <summary>
		/// Creates a quaternion which rotates from <paramref name="fromVector" /> to <paramref name="toVector" /> (normalized internally).
		/// If these vectors are known to be normalized or have magnitude close to 1, <see cref="M:Photon.Deterministic.FPQuaternion.FromToRotationSkipNormalize(Photon.Deterministic.FPVector3,Photon.Deterministic.FPVector3)" /> can be used for better performance.
		/// </summary>
		/// <param name="fromVector"></param>
		/// <param name="toVector"></param>
		/// <returns></returns>
		public static FPQuaternion FromToRotation(FPVector3 fromVector, FPVector3 toVector)
		{
			long num = (fromVector.X.RawValue * fromVector.X.RawValue + 32768 >> 16) + (fromVector.Y.RawValue * fromVector.Y.RawValue + 32768 >> 16) + (fromVector.Z.RawValue * fromVector.Z.RawValue + 32768 >> 16);
			if (num == 0L)
			{
				return Identity;
			}
			long num2 = (toVector.X.RawValue * toVector.X.RawValue + 32768 >> 16) + (toVector.Y.RawValue * toVector.Y.RawValue + 32768 >> 16) + (toVector.Z.RawValue * toVector.Z.RawValue + 32768 >> 16);
			if (num2 == 0L)
			{
				return Identity;
			}
			long num3 = 4294967296L / FPMath.SqrtRaw(num);
			long num4 = 4294967296L / FPMath.SqrtRaw(num2);
			FPVector3 fromVector2 = default(FPVector3);
			fromVector2.X.RawValue = fromVector.X.RawValue * num3 + 32768 >> 16;
			fromVector2.Y.RawValue = fromVector.Y.RawValue * num3 + 32768 >> 16;
			fromVector2.Z.RawValue = fromVector.Z.RawValue * num3 + 32768 >> 16;
			FPVector3 toVector2 = default(FPVector3);
			toVector2.X.RawValue = toVector.X.RawValue * num4 + 32768 >> 16;
			toVector2.Y.RawValue = toVector.Y.RawValue * num4 + 32768 >> 16;
			toVector2.Z.RawValue = toVector.Z.RawValue * num4 + 32768 >> 16;
			return FromToRotationSkipNormalize(fromVector2, toVector2);
		}

		/// <summary>
		/// Creates a quaternion which rotates from <paramref name="fromVector" /> to <paramref name="toVector" /> (not normalized internally).
		/// If these vectors are not known to be normalized, use <see cref="M:Photon.Deterministic.FPQuaternion.FromToRotation(Photon.Deterministic.FPVector3,Photon.Deterministic.FPVector3)" /> instead.
		/// </summary>
		/// <param name="fromVector"></param>
		/// <param name="toVector"></param>
		/// <returns></returns>
		public static FPQuaternion FromToRotationSkipNormalize(FPVector3 fromVector, FPVector3 toVector)
		{
			FPVector3 fPVector = default(FPVector3);
			fPVector.X.RawValue = fromVector.X.RawValue + toVector.X.RawValue;
			fPVector.Y.RawValue = fromVector.Y.RawValue + toVector.Y.RawValue;
			fPVector.Z.RawValue = fromVector.Z.RawValue + toVector.Z.RawValue;
			if (Math.Abs(fPVector.X.RawValue) <= 2 && Math.Abs(fPVector.Y.RawValue) <= 2 && Math.Abs(fPVector.Z.RawValue) <= 2)
			{
				long num = fromVector.X.RawValue * fromVector.X.RawValue + 32768 >> 16;
				FPVector3 b = default(FPVector3);
				if (num == 65536)
				{
					b.X.RawValue = fromVector.Z.RawValue;
					b.Y.RawValue = 0L;
					b.Z.RawValue = -fromVector.X.RawValue;
				}
				else
				{
					b.X.RawValue = 0L;
					b.Y.RawValue = -fromVector.Z.RawValue;
					b.Z.RawValue = fromVector.Y.RawValue;
				}
				b = FPVector3.Cross(fromVector, b);
				return RadianAxis(FP.Pi, b);
			}
			FPQuaternion value = default(FPQuaternion);
			value.X.RawValue = (fromVector.Y.RawValue * toVector.Z.RawValue + 32768 >> 16) - (fromVector.Z.RawValue * toVector.Y.RawValue + 32768 >> 16);
			value.Y.RawValue = (fromVector.Z.RawValue * toVector.X.RawValue + 32768 >> 16) - (fromVector.X.RawValue * toVector.Z.RawValue + 32768 >> 16);
			value.Z.RawValue = (fromVector.X.RawValue * toVector.Y.RawValue + 32768 >> 16) - (fromVector.Y.RawValue * toVector.X.RawValue + 32768 >> 16);
			value.W.RawValue = 65536 + (fromVector.X.RawValue * toVector.X.RawValue + 32768 >> 16) + (fromVector.Y.RawValue * toVector.Y.RawValue + 32768 >> 16) + (fromVector.Z.RawValue * toVector.Z.RawValue + 32768 >> 16);
			return NormalizeSmall(value);
		}

		/// <summary>
		/// Interpolates between <paramref name="a" /> and <paramref name="b" /> by <paramref name="t" /> and normalizes the result afterwards. The parameter <paramref name="t" /> is clamped to the range [0, 1].
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <param name="t"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static FPQuaternion Lerp(FPQuaternion a, FPQuaternion b, FP t)
		{
			if (t.RawValue < 0)
			{
				t.RawValue = 0L;
			}
			else if (t.RawValue > 65536)
			{
				t.RawValue = 65536L;
			}
			return LerpUnclamped(a, b, t);
		}

		/// <summary>
		/// Interpolates between <paramref name="a" /> and <paramref name="b" /> by <paramref name="t" /> and normalizes the result afterwards.
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <param name="t"></param>
		/// <returns></returns>
		public static FPQuaternion LerpUnclamped(FPQuaternion a, FPQuaternion b, FP t)
		{
			FP fP = default(FP);
			fP.RawValue = (a.W.RawValue * b.W.RawValue + 32768 >> 16) + (a.X.RawValue * b.X.RawValue + 32768 >> 16) + (a.Y.RawValue * b.Y.RawValue + 32768 >> 16) + (a.Z.RawValue * b.Z.RawValue + 32768 >> 16);
			if (fP.RawValue < 0)
			{
				b.X.RawValue = -b.X.RawValue;
				b.Y.RawValue = -b.Y.RawValue;
				b.Z.RawValue = -b.Z.RawValue;
				b.W.RawValue = -b.W.RawValue;
				fP.RawValue = -fP.RawValue;
			}
			long num = 65536 - t.RawValue;
			a.X.RawValue = a.X.RawValue * num + 32768 >> 16;
			a.Y.RawValue = a.Y.RawValue * num + 32768 >> 16;
			a.Z.RawValue = a.Z.RawValue * num + 32768 >> 16;
			a.W.RawValue = a.W.RawValue * num + 32768 >> 16;
			b.X.RawValue = b.X.RawValue * t.RawValue + 32768 >> 16;
			b.Y.RawValue = b.Y.RawValue * t.RawValue + 32768 >> 16;
			b.Z.RawValue = b.Z.RawValue * t.RawValue + 32768 >> 16;
			b.W.RawValue = b.W.RawValue * t.RawValue + 32768 >> 16;
			a.X.RawValue = a.X.RawValue + b.X.RawValue;
			a.Y.RawValue = a.Y.RawValue + b.Y.RawValue;
			a.Z.RawValue = a.Z.RawValue + b.Z.RawValue;
			a.W.RawValue = a.W.RawValue + b.W.RawValue;
			return Normalize(a);
		}

		/// <summary>
		/// Returns a rotation that rotates <paramref name="roll" /> radians around the z axis, <paramref name="pitch" /> radians around the x axis, and <paramref name="yaw" /> radians around the y axis.
		/// </summary>
		/// <param name="yaw">Yaw in radians</param>
		/// <param name="pitch">Pitch in radians</param>
		/// <param name="roll">Roll in radians</param>
		/// <returns></returns>
		public static FPQuaternion CreateFromYawPitchRoll(FP yaw, FP pitch, FP roll)
		{
			FP rad = default(FP);
			rad.RawValue = roll.RawValue / 2;
			FP rad2 = default(FP);
			rad2.RawValue = pitch.RawValue / 2;
			FP rad3 = default(FP);
			rad3.RawValue = yaw.RawValue / 2;
			FPMath.SinCosRaw(rad, out var sinRaw, out var cosRaw);
			FPMath.SinCosRaw(rad2, out var sinRaw2, out var cosRaw2);
			FPMath.SinCosRaw(rad3, out var sinRaw3, out var cosRaw3);
			FPQuaternion result = default(FPQuaternion);
			result.X.RawValue = ((cosRaw3 * sinRaw2 + 32768 >> 16) * cosRaw + 32768 >> 16) + ((sinRaw3 * cosRaw2 + 32768 >> 16) * sinRaw + 32768 >> 16);
			result.Y.RawValue = ((sinRaw3 * cosRaw2 + 32768 >> 16) * cosRaw + 32768 >> 16) - ((cosRaw3 * sinRaw2 + 32768 >> 16) * sinRaw + 32768 >> 16);
			result.Z.RawValue = ((cosRaw3 * cosRaw2 + 32768 >> 16) * sinRaw + 32768 >> 16) - ((sinRaw3 * sinRaw2 + 32768 >> 16) * cosRaw + 32768 >> 16);
			result.W.RawValue = ((cosRaw3 * cosRaw2 + 32768 >> 16) * cosRaw + 32768 >> 16) + ((sinRaw3 * sinRaw2 + 32768 >> 16) * sinRaw + 32768 >> 16);
			return result;
		}

		/// <summary>
		/// Returns the angle in degrees between two rotations <paramref name="a" /> and <paramref name="b" />.
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static FP Angle(FPQuaternion a, FPQuaternion b)
		{
			return FP.FromRaw(AngleRadians(a, b).RawValue * 3754936 + 32768 >> 16);
		}

		/// <summary>
		/// Returns the angle in radians between two rotations <paramref name="a" /> and <paramref name="b" />.
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static FP AngleRadians(FPQuaternion a, FPQuaternion b)
		{
			FP fP = default(FP);
			fP.RawValue = (a.X.RawValue * a.X.RawValue + 32768 >> 16) + (a.Y.RawValue * a.Y.RawValue + 32768 >> 16) + (a.Z.RawValue * a.Z.RawValue + 32768 >> 16) + (a.W.RawValue * a.W.RawValue + 32768 >> 16);
			long num = 4294967296L / fP.RawValue;
			a.X.RawValue = -a.X.RawValue;
			a.Y.RawValue = -a.Y.RawValue;
			a.Z.RawValue = -a.Z.RawValue;
			a.X.RawValue = a.X.RawValue * num + 32768 >> 16;
			a.Y.RawValue = a.Y.RawValue * num + 32768 >> 16;
			a.Z.RawValue = a.Z.RawValue * num + 32768 >> 16;
			a.W.RawValue = a.W.RawValue * num + 32768 >> 16;
			long rawValue = b.X.RawValue;
			long rawValue2 = b.Y.RawValue;
			long rawValue3 = b.Z.RawValue;
			long rawValue4 = b.W.RawValue;
			long rawValue5 = a.X.RawValue;
			long rawValue6 = a.Y.RawValue;
			long rawValue7 = a.Z.RawValue;
			long rawValue8 = a.W.RawValue;
			FPQuaternion fPQuaternion = default(FPQuaternion);
			fPQuaternion.X.RawValue = (rawValue5 * rawValue4 + 32768 >> 16) + (rawValue * rawValue8 + 32768 >> 16) + (rawValue6 * rawValue3 + 32768 >> 16) - (rawValue7 * rawValue2 + 32768 >> 16);
			fPQuaternion.Y.RawValue = (rawValue6 * rawValue4 + 32768 >> 16) + (rawValue2 * rawValue8 + 32768 >> 16) + (rawValue7 * rawValue + 32768 >> 16) - (rawValue5 * rawValue3 + 32768 >> 16);
			fPQuaternion.Z.RawValue = (rawValue7 * rawValue4 + 32768 >> 16) + (rawValue3 * rawValue8 + 32768 >> 16) + (rawValue5 * rawValue2 + 32768 >> 16) - (rawValue6 * rawValue + 32768 >> 16);
			fPQuaternion.W.RawValue = (rawValue8 * rawValue4 + 32768 >> 16) - ((rawValue5 * rawValue + 32768 >> 16) + (rawValue6 * rawValue2 + 32768 >> 16) + (rawValue7 * rawValue3 + 32768 >> 16));
			FP result = default(FP);
			result.RawValue = FPMath.Acos(fPQuaternion.W).RawValue << 1;
			if (result.RawValue > 205887)
			{
				result.RawValue = 411775 - result.RawValue;
			}
			return result;
		}

		/// <summary>
		/// Obsolete. Use one of the overloads that receive either only a forward direction (uses FPVector3.Up as up direction, not ortho-normalized)
		/// OR forward and up directions, which can be optionally ortho-normalized.
		/// </summary>
		[Obsolete("Use one of the overloads that receive either only a forward direction (uses FPVector3.Up as up direction, not ortho-normalized), OR forward and up directions, which can be optionally ortho-normalized.")]
		public static FPQuaternion LookRotation(FPVector3 forward, bool orthoNormalize)
		{
			return LookRotation(forward);
		}

		/// <summary>
		/// Creates a rotation with the specified <paramref name="forward" /> direction and <see cref="P:Photon.Deterministic.FPVector3.Up" />.
		/// </summary>
		/// <param name="forward"></param>
		/// <returns></returns>
		public static FPQuaternion LookRotation(FPVector3 forward)
		{
			return LookRotation(forward, FPVector3.Up);
		}

		/// <summary>
		/// Creates a rotation with the specified <paramref name="forward" /> and <paramref name="up" /> directions.
		/// </summary>
		/// <param name="forward"></param>
		/// <param name="up"></param>
		/// <param name="orthoNormalize"></param>
		/// <returns></returns>
		public static FPQuaternion LookRotation(FPVector3 forward, FPVector3 up, bool orthoNormalize = false)
		{
			forward = FPVector3.Normalize(forward, out var magnitude);
			if (magnitude.RawValue == 0L)
			{
				return Identity;
			}
			if (orthoNormalize)
			{
				up -= forward * FPVector3.Dot(up, forward);
				up = up.Normalized;
			}
			FPVector3 normalized = FPVector3.Cross(up, forward).Normalized;
			if (!orthoNormalize)
			{
				up = FPVector3.Cross(forward, normalized);
			}
			long num = Math.Abs(FPVector3.Dot(up, forward).RawValue);
			if (num >= 65536 || up.SqrMagnitude.RawValue == 0L)
			{
				return FromToRotationSkipNormalize(FPVector3.Forward, forward);
			}
			return FPMatrix3x3.FromColumns(normalized, up, forward).Rotation;
		}

		[Obsolete("SimpleLookAt is a cheaper version of LookRotation, but there are no extensive tests to ensure its correctness and equivalency to Unity's Quaternion.LookRotation as the latter has. We recommend using LookRotation instead.")]
		public static FPQuaternion SimpleLookAt(FPVector3 direction)
		{
			return SimpleLookAt(direction, FPVector3.Forward, FPVector3.Up);
		}

		[Obsolete("SimpleLookAt is a cheaper version of LookRotation, but there are no extensive tests to ensure its correctness and equivalency to Unity's Quaternion.LookRotation as the latter has. We recommend using LookRotation instead.")]
		public static FPQuaternion SimpleLookAt(FPVector3 direction, FPVector3 up)
		{
			return SimpleLookAt(direction, FPVector3.Forward, up);
		}

		[Obsolete("SimpleLookAt is a cheaper version of LookRotation, but there are no extensive tests to ensure its correctness and equivalency to Unity's Quaternion.LookRotation as the latter has. We recommend using LookRotation instead.")]
		public static FPQuaternion SimpleLookAt(FPVector3 direction, FPVector3 forward, FPVector3 up)
		{
			FPVector3 axis = FPVector3.Cross(forward, direction);
			if (axis.SqrMagnitude < FP.EN4)
			{
				axis = up;
			}
			FP value = FPVector3.Dot(forward, direction);
			FP angle = FPMath.Acos(value);
			return AngleAxis(angle, axis);
		}

		/// <summary>
		/// Spherically interpolates between <paramref name="from" /> and <paramref name="to" /> by <paramref name="t" /> and normalizes the result afterwards. <paramref name="t" /> is clamped to the range [0, 1].
		/// </summary>
		/// <remarks><see cref="T:Photon.Deterministic.FPLut" /> needs to be initialised.</remarks>
		/// <param name="from"></param>
		/// <param name="to"></param>
		/// <param name="t"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static FPQuaternion Slerp(FPQuaternion from, FPQuaternion to, FP t)
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
		/// Spherically interpolates between <paramref name="from" /> and <paramref name="to" /> by <paramref name="t" /> and normalizes the result afterwards.
		/// </summary>
		/// <remarks><see cref="T:Photon.Deterministic.FPLut" /> needs to be initialised.</remarks>
		/// <param name="from"></param>
		/// <param name="to"></param>
		/// <param name="t"></param>
		/// <returns></returns>
		public static FPQuaternion SlerpUnclamped(FPQuaternion from, FPQuaternion to, FP t)
		{
			FP value = default(FP);
			value.RawValue = (from.W.RawValue * to.W.RawValue + 32768 >> 16) + (from.X.RawValue * to.X.RawValue + 32768 >> 16) + (from.Y.RawValue * to.Y.RawValue + 32768 >> 16) + (from.Z.RawValue * to.Z.RawValue + 32768 >> 16);
			if (value.RawValue < 0)
			{
				to.X.RawValue = -to.X.RawValue;
				to.Y.RawValue = -to.Y.RawValue;
				to.Z.RawValue = -to.Z.RawValue;
				to.W.RawValue = -to.W.RawValue;
				value.RawValue = -value.RawValue;
			}
			if (value.RawValue >= 64881)
			{
				return LerpUnclamped(from, to, t);
			}
			FP rad = FPMath.Acos(value);
			FP rad2 = default(FP);
			rad2.RawValue = (65536 - t.RawValue) * rad.RawValue + 32768 >> 16;
			FP rad3 = default(FP);
			rad3.RawValue = t.RawValue * rad.RawValue + 32768 >> 16;
			FP fP = default(FP);
			fP.RawValue = 4294967296L / FPMath.Sin(rad).RawValue;
			rad2 = FPMath.Sin(rad2);
			rad3 = FPMath.Sin(rad3);
			from.X.RawValue = from.X.RawValue * rad2.RawValue + 32768 >> 16;
			from.Y.RawValue = from.Y.RawValue * rad2.RawValue + 32768 >> 16;
			from.Z.RawValue = from.Z.RawValue * rad2.RawValue + 32768 >> 16;
			from.W.RawValue = from.W.RawValue * rad2.RawValue + 32768 >> 16;
			to.X.RawValue = to.X.RawValue * rad3.RawValue + 32768 >> 16;
			to.Y.RawValue = to.Y.RawValue * rad3.RawValue + 32768 >> 16;
			to.Z.RawValue = to.Z.RawValue * rad3.RawValue + 32768 >> 16;
			to.W.RawValue = to.W.RawValue * rad3.RawValue + 32768 >> 16;
			from.X.RawValue += to.X.RawValue;
			from.Y.RawValue += to.Y.RawValue;
			from.Z.RawValue += to.Z.RawValue;
			from.W.RawValue += to.W.RawValue;
			from.X.RawValue = from.X.RawValue * fP.RawValue + 32768 >> 16;
			from.Y.RawValue = from.Y.RawValue * fP.RawValue + 32768 >> 16;
			from.Z.RawValue = from.Z.RawValue * fP.RawValue + 32768 >> 16;
			from.W.RawValue = from.W.RawValue * fP.RawValue + 32768 >> 16;
			return Normalize(from);
		}

		/// <summary>
		/// Rotates a rotation <paramref name="from" /> towards <paramref name="to" /> by an angular step of <paramref name="maxDegreesDelta" />.
		/// </summary>
		/// <param name="from"></param>
		/// <param name="to"></param>
		/// <param name="maxDegreesDelta"></param>
		/// <returns></returns>
		public static FPQuaternion RotateTowards(FPQuaternion from, FPQuaternion to, FP maxDegreesDelta)
		{
			FP value = default(FP);
			value.RawValue = (from.W.RawValue * to.W.RawValue + 32768 >> 16) + (from.X.RawValue * to.X.RawValue + 32768 >> 16) + (from.Y.RawValue * to.Y.RawValue + 32768 >> 16) + (from.Z.RawValue * to.Z.RawValue + 32768 >> 16);
			if (value.RawValue < 0)
			{
				to.X.RawValue = -to.X.RawValue;
				to.Y.RawValue = -to.Y.RawValue;
				to.Z.RawValue = -to.Z.RawValue;
				to.W.RawValue = -to.W.RawValue;
				value.RawValue = -value.RawValue;
			}
			FP fP = FPMath.Acos(value);
			FP fP2 = default(FP);
			fP2.RawValue = fP.RawValue << 1;
			maxDegreesDelta.RawValue = maxDegreesDelta.RawValue * FP.Deg2Rad.RawValue + 32768 >> 16;
			if (maxDegreesDelta.RawValue >= fP2.RawValue)
			{
				return to;
			}
			maxDegreesDelta.RawValue = (maxDegreesDelta.RawValue << 16) / fP2.RawValue;
			FP rad = (1 - maxDegreesDelta) * fP;
			FP rad2 = maxDegreesDelta * fP;
			rad = FPMath.Sin(rad);
			rad2 = FPMath.Sin(rad2);
			from.X.RawValue = from.X.RawValue * rad.RawValue + 32768 >> 16;
			from.Y.RawValue = from.Y.RawValue * rad.RawValue + 32768 >> 16;
			from.Z.RawValue = from.Z.RawValue * rad.RawValue + 32768 >> 16;
			from.W.RawValue = from.W.RawValue * rad.RawValue + 32768 >> 16;
			to.X.RawValue = to.X.RawValue * rad2.RawValue + 32768 >> 16;
			to.Y.RawValue = to.Y.RawValue * rad2.RawValue + 32768 >> 16;
			to.Z.RawValue = to.Z.RawValue * rad2.RawValue + 32768 >> 16;
			to.W.RawValue = to.W.RawValue * rad2.RawValue + 32768 >> 16;
			rad.RawValue = 4294967296L / FPMath.Sin(fP).RawValue;
			from.X.RawValue += to.X.RawValue;
			from.Y.RawValue += to.Y.RawValue;
			from.Z.RawValue += to.Z.RawValue;
			from.W.RawValue += to.W.RawValue;
			from.X.RawValue = from.X.RawValue * rad.RawValue + 32768 >> 16;
			from.Y.RawValue = from.Y.RawValue * rad.RawValue + 32768 >> 16;
			from.Z.RawValue = from.Z.RawValue * rad.RawValue + 32768 >> 16;
			from.W.RawValue = from.W.RawValue * rad.RawValue + 32768 >> 16;
			return from;
		}

		/// <summary>
		/// Returns a rotation that rotates <paramref name="z" /> degrees around the z axis, <paramref name="x" /> degrees around the x axis, and <paramref name="y" /> degrees around the y axis.
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="z"></param>
		/// <returns></returns>
		public static FPQuaternion Euler(FP x, FP y, FP z)
		{
			x.RawValue = x.RawValue * FP.Deg2Rad.RawValue + 32768 >> 16;
			y.RawValue = y.RawValue * FP.Deg2Rad.RawValue + 32768 >> 16;
			z.RawValue = z.RawValue * FP.Deg2Rad.RawValue + 32768 >> 16;
			return CreateFromYawPitchRoll(y, x, z);
		}

		/// <summary>
		/// Returns a rotation that rotates <paramref name="eulerAngles" />.z degrees around the z axis, <paramref name="eulerAngles" />.x degrees around the x axis, and <paramref name="eulerAngles" />.y degrees around the y axis.
		/// </summary>
		/// <param name="eulerAngles"></param>
		/// <returns></returns>
		public static FPQuaternion Euler(FPVector3 eulerAngles)
		{
			eulerAngles.X.RawValue = eulerAngles.X.RawValue * FP.Deg2Rad.RawValue + 32768 >> 16;
			eulerAngles.Y.RawValue = eulerAngles.Y.RawValue * FP.Deg2Rad.RawValue + 32768 >> 16;
			eulerAngles.Z.RawValue = eulerAngles.Z.RawValue * FP.Deg2Rad.RawValue + 32768 >> 16;
			return CreateFromYawPitchRoll(eulerAngles.Y, eulerAngles.X, eulerAngles.Z);
		}

		/// <summary>
		/// Creates a rotation which rotates <paramref name="angle" /> degrees around <paramref name="axis" />.
		/// </summary>
		/// <remarks><see cref="T:Photon.Deterministic.FPLut" /> needs to be initialised.</remarks>
		/// <param name="angle"></param>
		/// <param name="axis"></param>
		/// <returns></returns>
		public static FPQuaternion AngleAxis(FP angle, FPVector3 axis)
		{
			axis = FPVector3.Normalize(axis);
			FP rad = default(FP);
			rad.RawValue = (angle.RawValue * FP.Deg2Rad.RawValue + 32768 >> 16) / 2;
			FPMath.SinCos(rad, out var sin, out var cos);
			FPQuaternion result = default(FPQuaternion);
			result.X.RawValue = axis.X.RawValue * sin.RawValue + 32768 >> 16;
			result.Y.RawValue = axis.Y.RawValue * sin.RawValue + 32768 >> 16;
			result.Z.RawValue = axis.Z.RawValue * sin.RawValue + 32768 >> 16;
			result.W = cos;
			return result;
		}

		/// <summary>
		/// Creates a rotation which rotates <paramref name="radians" /> radians around <paramref name="axis" />.
		/// </summary>
		/// <remarks><see cref="T:Photon.Deterministic.FPLut" /> needs to be initialised.</remarks>
		/// <param name="radians"></param>
		/// <param name="axis"></param>
		/// <returns></returns>
		public static FPQuaternion RadianAxis(FP radians, FPVector3 axis)
		{
			axis = FPVector3.Normalize(axis);
			FP rad = default(FP);
			rad.RawValue = radians.RawValue / 2;
			FPMath.SinCos(rad, out var sin, out var cos);
			FPQuaternion result = default(FPQuaternion);
			result.X.RawValue = axis.X.RawValue * sin.RawValue + 32768 >> 16;
			result.Y.RawValue = axis.Y.RawValue * sin.RawValue + 32768 >> 16;
			result.Z.RawValue = axis.Z.RawValue * sin.RawValue + 32768 >> 16;
			result.W = cos;
			return result;
		}

		/// <summary>
		/// Returns the Inverse of rotation <paramref name="value" />. If <paramref name="value" /> is normalized it
		/// will be faster to call <see cref="M:Photon.Deterministic.FPQuaternion.Conjugate(Photon.Deterministic.FPQuaternion)" />. If <paramref name="value" />
		/// has a magnitude close to 0, <paramref name="value" /> will be returned.
		/// <remarks><see cref="T:Photon.Deterministic.FPLut" /> needs to be initialised.</remarks>
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static FPQuaternion Inverse(FPQuaternion value)
		{
			long magnitudeSqrRaw = value.MagnitudeSqrRaw;
			if (magnitudeSqrRaw == 0L)
			{
				return value;
			}
			magnitudeSqrRaw = 4294967296L / FPMath.SqrtRaw(magnitudeSqrRaw);
			FPQuaternion result = default(FPQuaternion);
			result.X.RawValue = -value.X.RawValue * magnitudeSqrRaw + 32768 >> 16;
			result.Y.RawValue = -value.Y.RawValue * magnitudeSqrRaw + 32768 >> 16;
			result.Z.RawValue = -value.Z.RawValue * magnitudeSqrRaw + 32768 >> 16;
			result.W.RawValue = value.W.RawValue * magnitudeSqrRaw + 32768 >> 16;
			return result;
		}

		/// <summary>
		/// Converts this quaternion <paramref name="value" /> to one with the same orientation but with a magnitude of 1. If <paramref name="value" />
		/// has a magnitude close to 0, <see cref="P:Photon.Deterministic.FPQuaternion.Identity" /> is returned.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static FPQuaternion Normalize(FPQuaternion value)
		{
			long magnitudeSqrRaw = value.MagnitudeSqrRaw;
			if (magnitudeSqrRaw == 0L)
			{
				return Identity;
			}
			magnitudeSqrRaw = 4294967296L / FPMath.SqrtRaw(magnitudeSqrRaw);
			FPQuaternion result = default(FPQuaternion);
			result.X.RawValue = value.X.RawValue * magnitudeSqrRaw + 32768 >> 16;
			result.Y.RawValue = value.Y.RawValue * magnitudeSqrRaw + 32768 >> 16;
			result.Z.RawValue = value.Z.RawValue * magnitudeSqrRaw + 32768 >> 16;
			result.W.RawValue = value.W.RawValue * magnitudeSqrRaw + 32768 >> 16;
			return result;
		}

		internal static FPQuaternion NormalizeSmall(FPQuaternion value)
		{
			ulong num = (ulong)(value.X.RawValue * value.X.RawValue + value.Y.RawValue * value.Y.RawValue + value.Z.RawValue * value.Z.RawValue + value.W.RawValue * value.W.RawValue);
			if (num == 0L)
			{
				return Identity;
			}
			FPMath.ExponentMantisaPair sqrtExponentMantissa = FPMath.GetSqrtExponentMantissa(num);
			long num2 = 17592186044416L / sqrtExponentMantissa.Mantissa;
			value.X.RawValue = value.X.RawValue * num2 >> 22 + sqrtExponentMantissa.Exponent - 8;
			value.Y.RawValue = value.Y.RawValue * num2 >> 22 + sqrtExponentMantissa.Exponent - 8;
			value.Z.RawValue = value.Z.RawValue * num2 >> 22 + sqrtExponentMantissa.Exponent - 8;
			value.W.RawValue = value.W.RawValue * num2 >> 22 + sqrtExponentMantissa.Exponent - 8;
			return value;
		}

		internal static FPVector3 ToEulerZXY(FPQuaternion value)
		{
			long rawValue = value.X.RawValue;
			long rawValue2 = value.Y.RawValue;
			long rawValue3 = value.Z.RawValue;
			long rawValue4 = value.W.RawValue;
			FP value2 = default(FP);
			value2.RawValue = (rawValue4 * rawValue >> 15) - (rawValue2 * rawValue3 >> 15);
			value2.RawValue = ((value2.RawValue > FP._1.RawValue) ? FP._1.RawValue : value2.RawValue);
			value2.RawValue = ((value2.RawValue < -FP._1.RawValue) ? (-FP._1.RawValue) : value2.RawValue);
			FPVector3 result = new FPVector3
			{
				X = FPMath.Asin(value2)
			};
			if (FPMath.Abs(value2).RawValue < 65533)
			{
				FP y = default(FP);
				y.RawValue = (rawValue * rawValue3 >> 15) + (rawValue4 * rawValue2 >> 15);
				FP x = default(FP);
				x.RawValue = FP._1.RawValue - (rawValue * rawValue >> 15) - (rawValue2 * rawValue2 >> 15);
				FP y2 = default(FP);
				y2.RawValue = (rawValue * rawValue2 >> 15) + (rawValue4 * rawValue3 >> 15);
				FP x2 = default(FP);
				x2.RawValue = FP._1.RawValue - (rawValue * rawValue >> 15) - (rawValue3 * rawValue3 >> 15);
				result.Y = FPMath.Atan2(y, x);
				result.Z = FPMath.Atan2(y2, x2);
			}
			else
			{
				FP y3 = default(FP);
				y3.RawValue = (rawValue4 * rawValue2 >> 15) - (rawValue * rawValue3 >> 15);
				FP x3 = default(FP);
				x3.RawValue = FP._1.RawValue - (rawValue2 * rawValue2 >> 15) - (rawValue3 * rawValue3 >> 15);
				result.Y = FPMath.Atan2(y3, x3);
				result.Z = 0;
			}
			result.X.RawValue = result.X.RawValue * FP.Rad2Deg.RawValue + 32768 >> 16;
			result.Y.RawValue = result.Y.RawValue * FP.Rad2Deg.RawValue + 32768 >> 16;
			result.Z.RawValue = result.Z.RawValue * FP.Rad2Deg.RawValue + 32768 >> 16;
			return result;
		}

		/// <summary>
		/// Computes product of two quaternions. Fully equivalent to Unity's Quaternion multiplication.
		/// See <see cref="M:Photon.Deterministic.FPQuaternion.Product(Photon.Deterministic.FPQuaternion,Photon.Deterministic.FPQuaternion)" /> for details.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static FPQuaternion operator *(FPQuaternion left, FPQuaternion right)
		{
			return Product(left, right);
		}

		/// <summary>
		/// Scales quaternion <paramref name="left" /> with <paramref name="right" />.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static FPQuaternion operator *(FPQuaternion left, FP right)
		{
			left.X.RawValue = left.X.RawValue * right.RawValue + 32768 >> 16;
			left.Y.RawValue = left.Y.RawValue * right.RawValue + 32768 >> 16;
			left.Z.RawValue = left.Z.RawValue * right.RawValue + 32768 >> 16;
			left.W.RawValue = left.W.RawValue * right.RawValue + 32768 >> 16;
			return left;
		}

		/// <summary>
		/// Scales quaternion <paramref name="right" /> with <paramref name="left" />.
		/// </summary>
		/// <param name="right"></param>
		/// <param name="left"></param>
		/// <returns></returns>
		public static FPQuaternion operator *(FP left, FPQuaternion right)
		{
			return right * left;
		}

		/// <summary>
		/// Adds each component of <paramref name="right" /> to <paramref name="left" />.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static FPQuaternion operator +(FPQuaternion left, FPQuaternion right)
		{
			left.X.RawValue += right.X.RawValue;
			left.Y.RawValue += right.Y.RawValue;
			left.Z.RawValue += right.Z.RawValue;
			left.W.RawValue += right.W.RawValue;
			return left;
		}

		/// <summary>
		/// Subtracts each component of <paramref name="right" /> from <paramref name="left" />.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static FPQuaternion operator -(FPQuaternion left, FPQuaternion right)
		{
			left.X.RawValue -= right.X.RawValue;
			left.Y.RawValue -= right.Y.RawValue;
			left.Z.RawValue -= right.Z.RawValue;
			left.W.RawValue -= right.W.RawValue;
			return left;
		}

		/// <summary>
		/// Rotates the point <paramref name="point" /> with rotation <paramref name="quat" />.
		/// </summary>
		/// <param name="quat"></param>
		/// <param name="point"></param>
		/// <returns></returns>
		public static FPVector3 operator *(FPQuaternion quat, FPVector3 point)
		{
			long rawValue = quat.X.RawValue;
			long rawValue2 = quat.Y.RawValue;
			long rawValue3 = quat.Z.RawValue;
			long rawValue4 = quat.W.RawValue;
			long rawValue5 = point.X.RawValue;
			long rawValue6 = point.Y.RawValue;
			long rawValue7 = point.Z.RawValue;
			long rawValue8 = FP._1.RawValue;
			long num = rawValue << 1;
			long num2 = rawValue2 << 1;
			long num3 = rawValue3 << 1;
			long num4 = rawValue * num + 32768 >> 16;
			long num5 = rawValue2 * num2 + 32768 >> 16;
			long num6 = rawValue3 * num3 + 32768 >> 16;
			long num7 = rawValue * num2 + 32768 >> 16;
			long num8 = rawValue * num3 + 32768 >> 16;
			long num9 = rawValue2 * num3 + 32768 >> 16;
			long num10 = rawValue4 * num + 32768 >> 16;
			long num11 = rawValue4 * num2 + 32768 >> 16;
			long num12 = rawValue4 * num3 + 32768 >> 16;
			FPVector3 result = default(FPVector3);
			result.X.RawValue = ((rawValue8 - (num5 + num6)) * rawValue5 + 32768 >> 16) + ((num7 - num12) * rawValue6 + 32768 >> 16) + ((num8 + num11) * rawValue7 + 32768 >> 16);
			result.Y.RawValue = ((num7 + num12) * rawValue5 + 32768 >> 16) + ((rawValue8 - (num4 + num6)) * rawValue6 + 32768 >> 16) + ((num9 - num10) * rawValue7 + 32768 >> 16);
			result.Z.RawValue = ((num8 - num11) * rawValue5 + 32768 >> 16) + ((num9 + num10) * rawValue6 + 32768 >> 16) + ((rawValue8 - (num4 + num5)) * rawValue7 + 32768 >> 16);
			return result;
		}
	}
}

