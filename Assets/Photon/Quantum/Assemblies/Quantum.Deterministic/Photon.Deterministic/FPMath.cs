using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Photon.Deterministic
{
	/// <summary>
	/// A collection of common math functions.
	/// </summary>
	/// \ingroup MathAPI
	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct FPMath
	{
		internal struct ExponentMantisaPair
		{
			public int Exponent;

			public int Mantissa;
		}

		internal const string LUT_NOT_LOADED_ERROR = "Math Lookup Tables (LUT) are not loaded and a trigonometric function was called. Call FPMathUtils.LoadLookupTables from Unity or manually call Init to load it.";

		/// <summary>
		/// Returns the sign of <paramref name="value" />.
		/// </summary>
		/// <param name="value"></param>
		/// <returns>1 when positive or zero, -1 when negative</returns>
		public static FP Sign(FP value)
		{
			if (value.RawValue >= 0)
			{
				return FP._1;
			}
			return FP.Minus_1;
		}

		/// <summary>
		/// Returns the sign of <paramref name="value" /> if it is non-zero.
		/// </summary>
		/// <param name="value"></param>
		/// <returns>1 when positive, 0 when zero, -1 when negative</returns>
		public static FP SignZero(FP value)
		{
			if (value.RawValue < 0)
			{
				return FP.Minus_1;
			}
			if (value.RawValue > 0)
			{
				return FP._1;
			}
			return FP._0;
		}

		/// <summary>
		/// Returns the sign of <paramref name="value" />.
		/// </summary>
		/// <param name="value"></param>
		/// <returns>1 when positive or zero, -1 when negative</returns>
		public static int SignInt(FP value)
		{
			if (value.RawValue >= 0)
			{
				return 1;
			}
			return -1;
		}

		/// <summary>
		/// Returns the sign of <paramref name="value" /> if it is non-zero.
		/// </summary>
		/// <param name="value"></param>
		/// <returns>1 when positive, 0 when zero, -1 when negative</returns>
		public static int SignZeroInt(FP value)
		{
			if (value.RawValue < 0)
			{
				return -1;
			}
			if (value.RawValue > 0)
			{
				return 1;
			}
			return 0;
		}

		/// <summary>
		/// Returns the next power of two that is equal to, or greater than, the argument.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static int NextPowerOfTwo(int value)
		{
			if (value <= 0)
			{
				throw new InvalidOperationException("Number must be positive");
			}
			uint num = (uint)value;
			num--;
			num |= num >> 1;
			num |= num >> 2;
			num |= num >> 4;
			num |= num >> 8;
			num |= num >> 16;
			return (int)(num + 1);
		}

		/// <summary>
		/// Returns the absolute value of the argument.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static FP Abs(FP value)
		{
			long num = value.RawValue >> 63;
			value.RawValue = (value.RawValue + num) ^ num;
			return value;
		}

		/// <summary>
		/// Returns <paramref name="value" /> rounded to the nearest integer.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static FP Round(FP value)
		{
			long num = value.RawValue & 0xFFFF;
			FP fP = Floor(value);
			if (num < 32768)
			{
				return fP;
			}
			if (num > 32768)
			{
				return fP + FP._1;
			}
			if ((fP.RawValue & FP._1.RawValue) != 0L)
			{
				return fP + FP._1;
			}
			return fP;
		}

		/// <summary>
		/// Returns <paramref name="value" /> rounded to the nearest integer.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static int RoundToInt(FP value)
		{
			if ((value.RawValue & 0xFFFF) >= FP._0_50.RawValue)
			{
				return (int)((value.RawValue >> 16) + 1);
			}
			return (int)(value.RawValue >> 16);
		}

		/// <summary>
		/// Returns the largest integer smaller than or equal to <paramref name="value" />.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static FP Floor(FP value)
		{
			value.RawValue &= -65536L;
			return value;
		}

		/// <inheritdoc cref="M:Photon.Deterministic.FPMath.Floor(Photon.Deterministic.FP)" />
		public static long FloorRaw(long value)
		{
			return value & -65536;
		}

		/// <summary>
		/// Returns the largest integer smaller than or equal to <paramref name="value" />.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static int FloorToInt(FP value)
		{
			return (int)(value.RawValue >> 16);
		}

		/// <summary>
		/// Returns the smallest integer larger than or equal to <paramref name="value" />.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static FP Ceiling(FP value)
		{
			if ((value.RawValue & 0xFFFF) != 0L)
			{
				value.RawValue = (value.RawValue & -65536) + 65536;
			}
			return value;
		}

		/// <summary>
		/// Returns the smallest integer larger than or equal to <paramref name="value" />.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static int CeilToInt(FP value)
		{
			if ((value.RawValue & 0xFFFF) >= 1)
			{
				return (int)((value.RawValue >> 16) + 1);
			}
			return (int)(value.RawValue >> 16);
		}

		/// <summary>
		/// Returns the largest of two or more values.
		/// </summary>
		/// <param name="val1"></param>
		/// <param name="val2"></param>
		/// <returns></returns>
		public static FP Max(FP val1, FP val2)
		{
			if (val1.RawValue <= val2.RawValue)
			{
				return val2;
			}
			return val1;
		}

		/// <summary>
		/// Returns the smallest of two or more values.
		/// </summary>
		/// <param name="val1"></param>
		/// <param name="val2"></param>
		/// <returns></returns>
		public static FP Min(FP val1, FP val2)
		{
			if (val1.RawValue >= val2.RawValue)
			{
				return val2;
			}
			return val1;
		}

		/// <summary>
		/// Returns the smallest of two or more values.
		/// </summary>
		/// <param name="numbers"></param>
		/// <returns></returns>
		public static FP Min(params FP[] numbers)
		{
			FP fP = numbers[0];
			for (int i = 1; i < numbers.Length; i++)
			{
				fP = Min(fP, numbers[i]);
			}
			return fP;
		}

		/// <summary>
		/// Returns the minimum of three values.
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <param name="c"></param>
		public static FP Min(FP a, FP b, FP c)
		{
			if (a > b)
			{
				a = b;
			}
			if (a > c)
			{
				a = c;
			}
			return a;
		}

		/// <summary>
		/// Returns the maximum of three values.
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <param name="c"></param>
		/// <returns></returns>
		public static FP Max(FP a, FP b, FP c)
		{
			if (a < b)
			{
				a = b;
			}
			if (a < c)
			{
				a = c;
			}
			return a;
		}

		/// <summary>
		/// Returns the largest of two or more values.
		/// </summary>
		/// <param name="numbers"></param>
		/// <returns></returns>
		public static FP Max(params FP[] numbers)
		{
			FP fP = numbers[0];
			for (int i = 1; i < numbers.Length; i++)
			{
				fP = Max(fP, numbers[i]);
			}
			return fP;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <param name="min"></param>
		/// <param name="max"></param>
		public static void MinMax(FP a, FP b, out FP min, out FP max)
		{
			if (a.RawValue < b.RawValue)
			{
				min = a;
				max = b;
			}
			else
			{
				min = b;
				max = a;
			}
		}

		/// <summary>
		/// Clamps the given value between the given minimum and maximum values.
		/// </summary>
		/// <param name="value"></param>
		/// <param name="min"></param>
		/// <param name="max"></param>
		/// <returns></returns>
		public static FP Clamp(FP value, FP min, FP max)
		{
			if (value.RawValue < min.RawValue)
			{
				return min;
			}
			if (value.RawValue > max.RawValue)
			{
				return max;
			}
			return value;
		}

		/// <summary>
		/// Clamps the given value between 0 and 1.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static FP Clamp01(FP value)
		{
			if (value.RawValue < 0)
			{
				return FP._0;
			}
			if (value.RawValue > 65536)
			{
				return FP._1;
			}
			return value;
		}

		/// <summary>
		/// Clamps the given value between the given minimum and maximum values.
		/// </summary>
		/// <param name="value"></param>
		/// <param name="min"></param>
		/// <param name="max"></param>
		/// <returns></returns>
		public static int Clamp(int value, int min, int max)
		{
			if (value < min)
			{
				return min;
			}
			if (value > max)
			{
				return max;
			}
			return value;
		}

		/// <summary>
		/// Clamps the given value between the given minimum and maximum values.
		/// </summary>
		/// <param name="value"></param>
		/// <param name="min"></param>
		/// <param name="max"></param>
		/// <returns></returns>
		public static long Clamp(long value, long min, long max)
		{
			if (value < min)
			{
				return min;
			}
			if (value > max)
			{
				return max;
			}
			return value;
		}

		/// <summary>
		/// Clamps the given value between <see cref="P:Photon.Deterministic.FP.UseableMin" /> and <see cref="P:Photon.Deterministic.FP.UseableMax" />.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static FP ClampUseable(FP value)
		{
			if (value.RawValue < int.MinValue)
			{
				return FP.FromRaw(-2147483648L);
			}
			if (value.RawValue > int.MaxValue)
			{
				return FP.FromRaw(2147483647L);
			}
			return value;
		}

		/// <summary>
		/// Returns the fractional part of the argument.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static FP Fraction(FP value)
		{
			value.RawValue &= 65535L;
			return value;
		}

		/// <summary>
		/// Loops the value <paramref name="t" />, so that it is never larger than <paramref name="length" /> and never smaller than 0.
		/// </summary>
		/// <param name="t"></param>
		/// <param name="length"></param>
		/// <returns></returns>
		public static FP Repeat(FP t, FP length)
		{
			FP result = default(FP);
			result.RawValue = RepeatRaw(t.RawValue, length.RawValue);
			return result;
		}

		internal static long RepeatRaw(long t, long length)
		{
			return t - (FloorRaw((t << 16) / length) * length + 32768 >> 16);
		}

		/// <summary>
		/// Linearly interpolates between <paramref name="start" /> and <paramref name="end" /> by <paramref name="t" />.
		/// <paramref name="t" /> is clamped to the range [0, 1]. The difference between <paramref name="start" /> and <paramref name="end" />
		/// is converted to a [-Pi/2, Pi/2] range.
		/// </summary>
		/// <param name="start"></param>
		/// <param name="end"></param>
		/// <param name="t"></param>
		/// <returns></returns>
		public static FP LerpRadians(FP start, FP end, FP t)
		{
			long num = RepeatRaw(end.RawValue - start.RawValue, FP.PiTimes2.RawValue);
			if (num > FP.Pi.RawValue)
			{
				num -= FP.PiTimes2.RawValue;
			}
			start.RawValue += num * Clamp01(t).RawValue + 32768 >> 16;
			return start;
		}

		/// <summary>
		/// Linearly interpolates between <paramref name="start" /> and <paramref name="end" /> by <paramref name="t" />.
		/// <paramref name="t" /> is clamped to the range [0, 1]
		/// </summary>
		/// <param name="start"></param>
		/// <param name="end"></param>
		/// <param name="t"></param>
		/// <returns></returns>
		public static FP Lerp(FP start, FP end, FP t)
		{
			if (t.RawValue < 0)
			{
				t.RawValue = 0L;
			}
			if (t.RawValue > 65536)
			{
				t.RawValue = 65536L;
			}
			start.RawValue += (end.RawValue - start.RawValue) * t.RawValue + 32768 >> 16;
			return start;
		}

		/// <summary>
		/// Linearly interpolates between <paramref name="start" /> and <paramref name="end" /> by <paramref name="t" />.
		/// </summary>
		/// <param name="start"></param>
		/// <param name="end"></param>
		/// <param name="t"></param>
		/// <returns></returns>
		public static FP LerpUnclamped(FP start, FP end, FP t)
		{
			start.RawValue += (end.RawValue - start.RawValue) * t.RawValue + 32768 >> 16;
			return start;
		}

		/// <summary>
		/// Calculates the linear parameter that produces the interpolant <paramref name="value" /> within the range [<paramref name="start" />, <paramref name="end" />].
		/// The result is clamped to the range [0, 1].
		/// <remarks>Returns 0 if <paramref name="start" /> and <paramref name="end" /> are equal.</remarks>
		/// </summary>
		/// <param name="start"></param>
		/// <param name="end"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public static FP InverseLerp(FP start, FP end, FP value)
		{
			if (start.RawValue == end.RawValue)
			{
				return default(FP);
			}
			value.RawValue = (value.RawValue - start.RawValue << 16) / (end.RawValue - start.RawValue);
			if (value.RawValue < 0)
			{
				value.RawValue = 0L;
			}
			if (value.RawValue > 65536)
			{
				value.RawValue = 65536L;
			}
			return value;
		}

		/// <summary>
		/// Calculates the linear parameter that produces the interpolant <paramref name="value" /> within the range [<paramref name="start" />, <paramref name="end" />].
		/// <remarks>The resultant factor is NOT clamped to the range [0, 1].</remarks>
		/// <remarks>Returns 0 if <paramref name="start" /> and <paramref name="end" /> are equal.</remarks>
		/// </summary>
		/// <param name="start"></param>
		/// <param name="end"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public static FP InverseLerpUnclamped(FP start, FP end, FP value)
		{
			if (start.RawValue == end.RawValue)
			{
				return default(FP);
			}
			value.RawValue = (value.RawValue - start.RawValue << 16) / (end.RawValue - start.RawValue);
			return value;
		}

		/// <summary>
		/// Moves a value <paramref name="from" /> towards <paramref name="to" /> by a value no greater than <paramref name="maxDelta" />.
		/// Negative values of <paramref name="maxDelta" /> push the value away from <paramref name="to" />.
		/// </summary>
		/// <param name="from"></param>
		/// <param name="to"></param>
		/// <param name="maxDelta"></param>
		/// <returns></returns>
		public static FP MoveTowards(FP from, FP to, FP maxDelta)
		{
			FP value = default(FP);
			value.RawValue = to.RawValue - from.RawValue;
			if (Abs(value).RawValue <= maxDelta.RawValue)
			{
				return to;
			}
			FP result = default(FP);
			if (value.RawValue < 0)
			{
				result.RawValue = from.RawValue - maxDelta.RawValue;
			}
			else
			{
				result.RawValue = from.RawValue + maxDelta.RawValue;
			}
			return result;
		}

		/// <summary>
		/// Interpolates between <paramref name="start" /> and <paramref name="end" /> with smoothing at the limits.
		/// Equivalent of calling <see cref="M:Photon.Deterministic.FPMath.Hermite(Photon.Deterministic.FP,Photon.Deterministic.FP,Photon.Deterministic.FP,Photon.Deterministic.FP,Photon.Deterministic.FP)" /> with tangents set to 0 and clamping <paramref name="t" /> between 0 and 1. 
		/// </summary>
		/// <param name="start"></param>
		/// <param name="end"></param>
		/// <param name="t"></param>
		/// <returns></returns>
		public static FP SmoothStep(FP start, FP end, FP t)
		{
			return Hermite(start, FP._0, end, FP._0, Clamp01(t));
		}

		/// <summary>
		/// Returns square root of <paramref name="value" />.
		/// </summary>
		/// <remarks><see cref="T:Photon.Deterministic.FPLut" /> needs to be initialised.</remarks>
		/// <param name="value"></param>
		/// <returns></returns>
		/// <exception cref="T:System.ArgumentOutOfRangeException">Thrown when <paramref name="value" /> is less than 0</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static FP Sqrt(FP value)
		{
			value.RawValue = SqrtRaw(value.RawValue);
			return value;
		}

		/// <summary>
		/// Returns square root of <paramref name="x" />.
		/// </summary>
		/// <param name="x">The value to square.</param>
		/// <returns></returns>
		/// <exception cref="T:System.ArgumentOutOfRangeException">The number is not positive.</exception>
		public static long SqrtRaw(long x)
		{
			if (x <= 65536)
			{
				if (x < 0)
				{
					throw new ArgumentOutOfRangeException("x", $"The number has to be positive: {x}");
				}
				return FPLut.sqrt_aprox_lut[x] >> 6;
			}
			long num = x;
			int num2 = 0;
			if (num >> 32 != 0L)
			{
				num >>= 32;
				num2 += 32;
			}
			if (num >> 16 != 0L)
			{
				num >>= 16;
				num2 += 16;
			}
			if (num >> 8 != 0L)
			{
				num >>= 8;
				num2 += 8;
			}
			if (num >> 4 != 0L)
			{
				num >>= 4;
				num2 += 4;
			}
			if (num >> 2 != 0L)
			{
				num2 += 2;
			}
			int num3 = num2 - 16 + 2;
			int num4 = FPLut.sqrt_aprox_lut[x >> num3];
			num = (long)num4 << (num3 >> 1);
			return num >> 6;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static ExponentMantisaPair GetSqrtExponentMantissa(ulong x)
		{
			if (x <= 65536)
			{
				return new ExponentMantisaPair
				{
					Exponent = 0,
					Mantissa = FPLut.sqrt_aprox_lut[x]
				};
			}
			ulong num = x;
			int num2 = 0;
			if (num >> 32 != 0L)
			{
				num >>= 32;
				num2 += 32;
			}
			if (num >> 16 != 0L)
			{
				num >>= 16;
				num2 += 16;
			}
			if (num >> 8 != 0L)
			{
				num >>= 8;
				num2 += 8;
			}
			if (num >> 4 != 0L)
			{
				num >>= 4;
				num2 += 4;
			}
			if (num >> 2 != 0L)
			{
				num2 += 2;
			}
			int num3 = num2 - 16 + 2;
			return new ExponentMantisaPair
			{
				Exponent = num3 >> 1,
				Mantissa = FPLut.sqrt_aprox_lut[x >> num3]
			};
		}

		/// <summary>
		/// Performs barycentric interpolation.
		/// </summary>
		/// <param name="value1"></param>
		/// <param name="value2"></param>
		/// <param name="value3"></param>
		/// <param name="t1"></param>
		/// <param name="t2"></param>
		/// <returns> <paramref name="value1" /> + (<paramref name="value2" /> - <paramref name="value1" />) * <paramref name="t1" /> + (<paramref name="value3" /> - <paramref name="value1" />) * <paramref name="t2" />
		/// </returns>
		public static FP Barycentric(FP value1, FP value2, FP value3, FP t1, FP t2)
		{
			value1.RawValue = value1.RawValue + ((value2.RawValue - value1.RawValue) * t1.RawValue + 32768 >> 16) + ((value3.RawValue - value1.RawValue) * t2.RawValue + 32768 >> 16);
			return value1;
		}

		/// <summary>
		/// Performs Cotmull-Rom interpolation.
		/// </summary>
		/// <param name="value1"></param>
		/// <param name="value2"></param>
		/// <param name="value3"></param>
		/// <param name="value4"></param>
		/// <param name="t"></param>
		/// <returns></returns>
		public static FP CatmullRom(FP value1, FP value2, FP value3, FP value4, FP t)
		{
			FP fP = default(FP);
			fP.RawValue = t.RawValue * t.RawValue + 32768 >> 16;
			FP fP2 = default(FP);
			fP2.RawValue = fP.RawValue * t.RawValue + 32768 >> 16;
			value1.RawValue = (value2.RawValue << 1) + ((value3.RawValue - value1.RawValue) * t.RawValue + 32768 >> 16) + (((value1.RawValue << 1) - (FP._5.RawValue * value2.RawValue + 32768 >> 16) + (value3.RawValue << 2) - value4.RawValue) * fP.RawValue + 32768 >> 16) + (((FP._3.RawValue * value2.RawValue + 32768 >> 16) - value1.RawValue - (FP._3.RawValue * value3.RawValue + 32768 >> 16) + value4.RawValue) * fP2.RawValue + 32768 >> 16) >> 1;
			return value1;
		}

		/// <summary>
		/// Performs cubic Hermite interpolation.
		/// </summary>
		/// <param name="value1"></param>
		/// <param name="tangent1"></param>
		/// <param name="value2"></param>
		/// <param name="tangent2"></param>
		/// <param name="t"></param>
		/// <returns></returns>
		public static FP Hermite(FP value1, FP tangent1, FP value2, FP tangent2, FP t)
		{
			FP fP = default(FP);
			fP.RawValue = t.RawValue * t.RawValue + 32768 >> 16;
			FP fP2 = default(FP);
			fP2.RawValue = fP.RawValue * t.RawValue + 32768 >> 16;
			if (t == FP._0)
			{
				return value1;
			}
			if (t == FP._1)
			{
				return value2;
			}
			FP result = default(FP);
			result.RawValue = (((value1.RawValue << 1) - (value2.RawValue << 1) + tangent2.RawValue + tangent1.RawValue) * fP2.RawValue + 32768 >> 16) + (((FP._3.RawValue * value2.RawValue + 32768 >> 16) - (FP._3.RawValue * value1.RawValue + 32768 >> 16) - (tangent1.RawValue << 1) - tangent2.RawValue) * fP.RawValue + 32768 >> 16) + (tangent1.RawValue * t.RawValue + 32768 >> 16) + value1.RawValue;
			return result;
		}

		/// <summary>
		/// Performs modulo operation without forcing the sign of the dividend: So that ModuloClamped(-9, 10) = 1.
		/// </summary>
		/// <param name="a">Dividend</param>
		/// <param name="n">Divisor</param>
		/// <returns>Remainder after division</returns>
		/// <exception cref="T:System.InvalidOperationException">When n &gt; Int64.MaxValue &gt;&gt; 2 or n &lt; Int64.MinValue &gt;&gt; 2</exception>
		/// <exception cref="T:System.DivideByZeroException">When n == 0</exception>
		public static long ModuloClamped(long a, long n)
		{
			if (n > 2305843009213693951L)
			{
				throw new InvalidOperationException("N too big");
			}
			if (n < -2305843009213693952L)
			{
				throw new InvalidOperationException("N too small");
			}
			return (a % n + n) % n;
		}

		/// <summary>
		/// Performs modulo operation without forcing the sign of the dividend: So that ModuloClamped(-9, 10) = 1.
		/// </summary>
		/// <param name="a">Dividend</param>
		/// <param name="n">Divisor</param>
		/// <returns>Remainder after division</returns>
		/// <exception cref="T:System.InvalidOperationException">When n &gt; <see cref="P:Photon.Deterministic.FP.UseableMax" /> or n &lt; <see cref="P:Photon.Deterministic.FP.UseableMin" /></exception>
		/// <exception cref="T:System.DivideByZeroException">When n == 0</exception>
		public static FP ModuloClamped(FP a, FP n)
		{
			if (n > FP.UseableMax)
			{
				throw new InvalidOperationException("N too big");
			}
			if (n < FP.UseableMin)
			{
				throw new InvalidOperationException("N too small");
			}
			return new FP
			{
				RawValue = (a.RawValue % n.RawValue + n.RawValue) % n.RawValue
			};
		}

		/// <summary>
		/// Calculates the smallest signed angle between any two angles. F.e. angle between -179 and 179 is -2. Rotation is ccw.
		/// </summary>
		/// <param name="source">Source angle in degrees</param>
		/// <param name="target">Target angle in degrees</param>
		/// <returns></returns>
		public static FP AngleBetweenDegrees(FP source, FP target)
		{
			long num = target.RawValue - source.RawValue;
			return new FP
			{
				RawValue = ModuloClamped(num + 11796480, 23592960L) - 11796480
			};
		}

		/// <summary>
		/// Same as AngleBetweenDegrees using Raw optimization.
		/// </summary>
		/// <param name="source">Source angle in degrees (Raw)</param>
		/// <param name="target">Target angle in degrees (Raw)</param>
		/// <returns></returns>
		public static long AngleBetweenDegreesRaw(long source, long target)
		{
			long num = target - source;
			return ModuloClamped(num + 11796480, 23592960L) - 11796480;
		}

		/// <summary>
		/// Calculates the smallest signed angle between any two angles.
		/// </summary>
		/// <param name="source">Source angle in radians</param>
		/// <param name="target">Target angle in radians</param>
		/// <returns></returns>
		public static FP AngleBetweenRadians(FP source, FP target)
		{
			long num = target.RawValue - source.RawValue;
			return new FP
			{
				RawValue = ModuloClamped(num + FP.Pi.RawValue, FP.PiTimes2.RawValue) - FP.Pi.RawValue
			};
		}

		/// <summary>
		/// Same as AngleBetweenDegrees using Raw optimization.
		/// </summary>
		/// <param name="source">Source angle in radians (Raw)</param>
		/// <param name="target">Target angle in radians (Raw)</param>
		/// <returns></returns>
		public static long AngleBetweenRadiansRaw(long source, long target)
		{
			long num = target - source;
			return ModuloClamped(num + FP.Pi.RawValue, FP.PiTimes2.RawValue) - FP.Pi.RawValue;
		}

		/// <summary>
		/// Returns floor of the logarithm of <paramref name="value" /> in base 2. It is much
		/// faster than calling <see cref="M:Photon.Deterministic.FPMath.Log2(Photon.Deterministic.FP)" /> and then <see cref="M:Photon.Deterministic.FPMath.FloorToInt(Photon.Deterministic.FP)" />
		/// </summary>
		/// <remarks><see cref="T:Photon.Deterministic.FPLut" /> needs to be initialised.</remarks>
		/// <param name="value"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int Log2FloorToInt(FP value)
		{
			return Log2FloorToIntRaw(value.RawValue);
		}

		/// <summary>
		/// Returns celining of the logarithm of <paramref name="value" /> in base 2. It is much
		/// faster than calling <see cref="M:Photon.Deterministic.FPMath.Log2(Photon.Deterministic.FP)" /> and then <see cref="M:Photon.Deterministic.FPMath.CeilToInt(Photon.Deterministic.FP)" />
		/// </summary>
		/// <remarks><see cref="T:Photon.Deterministic.FPLut" /> needs to be initialised.</remarks>
		/// <param name="value"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int Log2CeilingToInt(FP value)
		{
			int num = 0;
			if ((value.RawValue & (value.RawValue - 1)) != 0L)
			{
				num = 1;
			}
			return Log2FloorToIntRaw(value.RawValue) + num;
		}

		/// <summary>
		/// Returns logarithm of <paramref name="value" /> in base 2.
		/// </summary>
		/// <remarks><see cref="T:Photon.Deterministic.FPLut" /> needs to be initialised.</remarks>
		/// <param name="value"></param>
		/// <returns></returns>
		public static FP Log2(FP value)
		{
			value.RawValue = Log2RawAdditionalPrecision(value.RawValue) >> 15;
			return value;
		}

		/// <summary>
		/// Returns natural logarithm of <paramref name="value" />.
		/// </summary>
		/// <remarks><see cref="T:Photon.Deterministic.FPLut" /> needs to be initialised.</remarks>
		/// <param name="value"></param>
		/// <returns></returns>
		public static FP Ln(FP value)
		{
			value.RawValue = Log2RawAdditionalPrecision(value.RawValue);
			value.RawValue >>= 6;
			value.RawValue = FPHighPrecisionDivisor.RawDiv(value.RawValue, 6196328018L);
			value.RawValue >>= 9;
			return value;
		}

		/// <summary>
		/// Returns logarithm of <paramref name="value" /> in base 10.
		/// </summary>
		/// <remarks><see cref="T:Photon.Deterministic.FPLut" /> needs to be initialised.</remarks>
		/// <param name="value"></param>
		/// <returns></returns>
		public static FP Log10(FP value)
		{
			value.RawValue = Log2RawAdditionalPrecision(value.RawValue);
			value.RawValue >>= 6;
			value.RawValue = FPHighPrecisionDivisor.RawDiv(value.RawValue, 14267572527L);
			value.RawValue >>= 9;
			return value;
		}

		/// <summary>
		/// Returns logarithm of <paramref name="value" /> in base <paramref name="logBase" />.
		/// It is much more performant and precise to use Log2, Log10 and Ln if <paramref name="logBase" /> is 2, 10 or e.
		/// </summary>
		/// <remarks><see cref="T:Photon.Deterministic.FPLut" /> needs to be initialised.</remarks>
		/// <param name="value"></param>
		/// <param name="logBase"></param>
		/// <returns></returns>
		public static FP Log(FP value, FP logBase)
		{
			value.RawValue = Log2RawAdditionalPrecision(value.RawValue);
			logBase.RawValue = Log2RawAdditionalPrecision(logBase.RawValue);
			value.RawValue = (value.RawValue << 16) / logBase.RawValue;
			return value;
		}

		private static int Log2FloorToIntRaw(long x)
		{
			if (x <= 0)
			{
				throw new ArgumentOutOfRangeException("x", "The number has to be positive");
			}
			long num = x;
			int num2 = 0;
			if (num >> 32 != 0L)
			{
				num >>= 32;
				num2 += 32;
			}
			if (num >> 16 != 0L)
			{
				num >>= 16;
				num2 += 16;
			}
			if (num >> 8 != 0L)
			{
				num >>= 8;
				num2 += 8;
			}
			if (num >> 4 != 0L)
			{
				num >>= 4;
				num2 += 4;
			}
			if (num >> 2 != 0L)
			{
				num >>= 2;
				num2 += 2;
			}
			if (num >> 1 != 0L)
			{
				num2++;
			}
			return num2 - 16;
		}

		private static long Log2RawAdditionalPrecision(long x)
		{
			uint[] log2_approx_lut = FPLut.log2_approx_lut;
			int num = Log2FloorToIntRaw(x);
			uint num2 = (uint)((int)x << 48 - num);
			uint num3 = num2 >> 26;
			uint num4 = log2_approx_lut[num3 + 1] - log2_approx_lut[num3];
			uint num5 = log2_approx_lut[num3 + 2] - log2_approx_lut[num3];
			int num6 = (int)((num5 >> 1) - num4);
			int num7 = (int)((num4 << 1) - (num5 >> 1));
			uint num8 = num2 & 0x3FFFFFF;
			int num9 = (int)(((num6 * num8 >> 26) + num7) * num8 >> 26);
			uint num10 = (uint)(log2_approx_lut[num3] + num9);
			num10 += 16384;
			long num11 = (long)num << 31;
			return num11 + num10;
		}

		/// <summary>
		/// Returns e raised to the specified power. The max relative error is ~0.3% in the range of [-6, 32].
		/// </summary>
		/// <param name="x"></param>
		/// <returns></returns>
		public static FP Exp(FP x)
		{
			long num = x.RawValue >> 16;
			long num2 = x.RawValue & 0xFFFF;
			if (num2 >= 32768)
			{
				num2 -= 65536;
				num++;
			}
			if (num < -30)
			{
				return 0;
			}
			if (num >= 33)
			{
				return FP.MaxValue;
			}
			long num3 = 65536L;
			num3 += num2;
			num3 += num2 * num2 / 2 >> 16;
			num3 += num2 * num2 * num2 / 6 >> 32;
			num3 += num2 * num2 * num2 * num2 / 24 >> 48;
			long num4 = FPLut.exp_integral_lut[30 + num];
			FP result = default(FP);
			if (num < 0)
			{
				long num5 = num3 * num4;
				result.RawValue = num5 + 2199023255552L >> 42;
			}
			else if (num > 20)
			{
				result.RawValue = num3 * num4;
			}
			else
			{
				result.RawValue = num3 * num4 >> 16;
			}
			return result;
		}

		/// <summary>
		/// Returns the sine of angle <paramref name="rad" />.
		/// </summary>
		/// <remarks><see cref="T:Photon.Deterministic.FPLut" /> needs to be initialised.</remarks>
		/// <param name="rad">Angle in radians</param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static FP Sin(FP rad)
		{
			long rawValue = rad.RawValue;
			long num = rawValue >> 63;
			rawValue = (rawValue + num) ^ num;
			rawValue %= 411775;
			long num2 = FPLut.sin_cos_lut[rawValue];
			rawValue = ((int)(num2 & 0xFFFFFFFFu) + num) ^ num;
			rad.RawValue = rawValue;
			return rad;
		}

		/// <summary>
		/// Returns the high precision sine of angle <paramref name="rad" />.
		/// </summary>
		/// <remarks><see cref="T:Photon.Deterministic.FPLut" /> needs to be initialised.</remarks>
		/// <param name="rad">Angle in radians</param>
		/// <returns></returns>
		public static FP SinHighPrecision(FP rad)
		{
			long rawValue = rad.RawValue;
			long num = rawValue >> 63;
			rawValue = (rawValue + num) ^ num;
			rawValue <<= 16;
			rawValue %= 26986075409L;
			rawValue >>= 16;
			long num2 = FPLut.sin_cos_lut[rawValue];
			rawValue = ((int)(num2 & 0xFFFFFFFFu) + num) ^ num;
			rad.RawValue = rawValue;
			return rad;
		}

		/// <summary>
		/// Returns the cosine of angle <paramref name="rad" />.
		/// </summary>
		/// <remarks><see cref="T:Photon.Deterministic.FPLut" /> needs to be initialised.</remarks>
		/// <param name="rad">Angle in radians</param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static FP Cos(FP rad)
		{
			long rawValue = rad.RawValue;
			long num = rawValue >> 63;
			rawValue = (rawValue + num) ^ num;
			rawValue %= 411775;
			long num2 = FPLut.sin_cos_lut[rawValue];
			rawValue = num2 >> 32;
			rad.RawValue = rawValue;
			return rad;
		}

		/// <summary>
		/// Returns the high precision cosine of angle <paramref name="rad" />.
		/// </summary>
		/// <remarks><see cref="T:Photon.Deterministic.FPLut" /> needs to be initialised.</remarks>
		/// <param name="rad">Angle in radians</param>
		/// <returns></returns>
		public static FP CosHighPrecision(FP rad)
		{
			long rawValue = rad.RawValue;
			long num = rawValue >> 63;
			rawValue = (rawValue + num) ^ num;
			rawValue <<= 16;
			rawValue %= 26986075409L;
			rawValue >>= 16;
			long num2 = FPLut.sin_cos_lut[rawValue];
			rawValue = num2 >> 32;
			rad.RawValue = rawValue;
			return rad;
		}

		/// <summary>
		/// Calculates sine and cosine of angle <paramref name="rad" />. It is faster than 
		/// calling <see cref="M:Photon.Deterministic.FPMath.Sin(Photon.Deterministic.FP)" />  and <see cref="M:Photon.Deterministic.FPMath.Cos(Photon.Deterministic.FP)" /> separately.
		/// </summary>
		/// <remarks><see cref="T:Photon.Deterministic.FPLut" /> needs to be initialised.</remarks>
		/// <param name="rad">Angle in radians</param>
		/// <param name="sin"></param>
		/// <param name="cos"></param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void SinCos(FP rad, out FP sin, out FP cos)
		{
			long rawValue = rad.RawValue;
			long num = rawValue >> 63;
			rawValue = (rawValue + num) ^ num;
			rawValue %= 411775;
			long num2 = FPLut.sin_cos_lut[rawValue];
			cos.RawValue = num2 >> 32;
			sin.RawValue = ((int)(num2 & 0xFFFFFFFFu) + num) ^ num;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void SinCosRaw(FP rad, out long sinRaw, out long cosRaw)
		{
			long rawValue = rad.RawValue;
			long num = rawValue >> 63;
			rawValue = (rawValue + num) ^ num;
			rawValue %= 411775;
			long num2 = FPLut.sin_cos_lut[rawValue];
			cosRaw = num2 >> 32;
			sinRaw = ((int)(num2 & 0xFFFFFFFFu) + num) ^ num;
		}

		/// <summary>
		/// Calculates high precision sine and cosine of angle <paramref name="rad" />. It is faster than 
		/// calling <see cref="M:Photon.Deterministic.FPMath.SinHighPrecision(Photon.Deterministic.FP)" />  and <see cref="M:Photon.Deterministic.FPMath.CosHighPrecision(Photon.Deterministic.FP)" /> separately.
		/// </summary>
		/// <remarks><see cref="T:Photon.Deterministic.FPLut" /> needs to be initialised.</remarks>
		/// <param name="rad">Angle in radians</param>
		/// <param name="sin"></param>
		/// <param name="cos"></param>
		public static void SinCosHighPrecision(FP rad, out FP sin, out FP cos)
		{
			long rawValue = rad.RawValue;
			long num = rawValue >> 63;
			rawValue = (rawValue + num) ^ num;
			rawValue <<= 16;
			rawValue %= 26986075409L;
			rawValue >>= 16;
			long num2 = FPLut.sin_cos_lut[rawValue];
			cos.RawValue = num2 >> 32;
			sin.RawValue = ((int)(num2 & 0xFFFFFFFFu) + num) ^ num;
		}

		/// <summary>
		/// Returns the tangent of angle <paramref name="rad" />.
		/// </summary>
		/// <remarks><see cref="T:Photon.Deterministic.FPLut" /> needs to be initialised.</remarks>
		/// <param name="rad">Angle in radians</param>
		/// <returns></returns>
		public static FP Tan(FP rad)
		{
			if (rad.RawValue < -205887)
			{
				rad.RawValue %= -205887L;
			}
			if (rad.RawValue > 205887)
			{
				rad.RawValue %= 205887L;
			}
			rad.RawValue = FPLut.tan_lut[rad.RawValue + 205887];
			return rad;
		}

		/// <summary>
		/// Returns the arc-sine of <paramref name="value" /> - the angle in radians whose sine is <paramref name="value" />.
		/// </summary>
		/// <remarks><see cref="T:Photon.Deterministic.FPLut" /> needs to be initialised.</remarks>
		/// <param name="value"></param>
		/// <returns></returns>
		public static FP Asin(FP value)
		{
			if (value.RawValue < -65536 || value.RawValue > 65536)
			{
				return FP.MinValue;
			}
			value.RawValue = FPLut.asin_lut[value.RawValue + 65536];
			return value;
		}

		/// <summary>
		/// Returns the arc-cosine of <paramref name="value" /> - the angle in radians whose cosine is <paramref name="value" />.
		/// </summary>
		/// <remarks><see cref="T:Photon.Deterministic.FPLut" /> needs to be initialised.</remarks>
		/// <param name="value"></param>
		/// <returns></returns>
		public static FP Acos(FP value)
		{
			if (value.RawValue < -65536 || value.RawValue > 65536)
			{
				return FP.MinValue;
			}
			value.RawValue = FPLut.acos_lut[value.RawValue + 65536];
			return value;
		}

		/// <summary>
		/// Returns the arc-tangent of <paramref name="value" /> - the angle in radians whose tangent is <paramref name="value" />.
		/// </summary>
		/// <remarks><see cref="T:Photon.Deterministic.FPLut" /> needs to be initialised.</remarks>
		/// <param name="value"></param>
		/// <returns></returns>
		public static FP Atan(FP value)
		{
			long num = value.RawValue >> 63;
			value.RawValue = (value.RawValue + num) ^ num;
			if (value.RawValue <= 393216)
			{
				value.RawValue = (FPLut.atan_lut[value.RawValue] + num) ^ num;
				return value;
			}
			if (value.RawValue <= 16384000)
			{
				value.RawValue = value.RawValue - 393216 >> 12;
				value.RawValue = (FPLut.atan_lut[393216 + value.RawValue] + num) ^ num;
				return value;
			}
			if (value.RawValue <= 655360000)
			{
				value.RawValue = value.RawValue - 16384000 >> 20;
				value.RawValue = (FPLut.atan_lut[397120 + value.RawValue] + num) ^ num;
				return value;
			}
			value.RawValue = (FPLut.atan_lut[FPLut.atan_lut.Length - 1] + num) ^ num;
			return value;
		}

		/// <summary>
		/// Returns the angle in radians whose <see cref="M:Photon.Deterministic.FPMath.Tan(Photon.Deterministic.FP)" /> is <paramref name="y" />/<paramref name="x" />. This function returns correct angle even if x is zero.
		/// </summary>
		/// <remarks><see cref="T:Photon.Deterministic.FPLut" /> needs to be initialised.</remarks>
		/// <param name="y"></param>
		/// <param name="x"></param>
		/// <returns></returns>
		public static FP Atan2(FP y, FP x)
		{
			if (x.RawValue > 0)
			{
				return Atan(y / x);
			}
			if (x.RawValue < 0)
			{
				if (y.RawValue >= 0)
				{
					y.RawValue = Atan(y / x).RawValue + 205887;
				}
				else
				{
					y.RawValue = Atan(y / x).RawValue - 205887;
				}
				return y;
			}
			if (y.RawValue > 0)
			{
				return FP.PiOver2;
			}
			if (y.RawValue == 0L)
			{
				return FP._0;
			}
			return -FP.PiOver2;
		}
	}
}

