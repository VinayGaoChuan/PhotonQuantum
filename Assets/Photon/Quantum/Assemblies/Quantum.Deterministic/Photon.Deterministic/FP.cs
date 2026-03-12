using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Photon.Deterministic
{
	/// <summary>
	/// A fixed-point number. 16 lower bits are used for the decimal part, 48 for the integral part.
	/// <para> It provides various methods for performing mathematical operations and converting between different data types.</para>
	/// <para>However, a majority of internal code and the multiplication operator perform fast multiplication,
	/// where the result can use at most 32 bits for the integral part and overflows are not detected.
	/// This means that you should stay in <see cref="T:System.Int16" /> range.
	/// <seealso cref="P:Photon.Deterministic.FP.UseableMax" />
	/// <seealso cref="P:Photon.Deterministic.FP.UseableMin" /></para>
	/// </summary>
	/// \ingroup MathAPI
	/// <remarks>
	/// The precision of the decimal part is 5 digits.
	/// The decimal fraction normalizer is 1E5.
	/// The size of an FP object is 8 bytes.
	/// The raw value of one is equal to FPLut.ONE.
	/// The raw value of zero is 0.
	/// The precision value is equal to FPLut.PRECISION.
	/// The number of bits in an FP object is equal to the size of a long (64 bits).
	/// The MulRound constant is 0.
	/// The MulShift constant is equal to the precision value.
	/// The MulShiftTrunc constant is equal to the precision value.
	/// The UsesRoundedConstants constant is either <see langword="true" /> or <see langword="false" />, depending on the value of PHOTONDETERMINISTIC_FP_OLD_CONSTANTS.
	/// </remarks>
	/// <seealso cref="T:Photon.Deterministic.FPLut" />
	/// <see langword="true" />
	[Serializable]
	[StructLayout(LayoutKind.Explicit)]
	public struct FP : IEquatable<FP>, IComparable<FP>
	{
		/// <summary>
		/// Holds <see cref="T:Photon.Deterministic.FP" /> constants in raw (long) form.
		/// </summary>
		public static class Raw
		{
			/// <summary>
			/// The smallest FP unit that is not 0.
			/// <para>Closest double: 1.52587890625E-05</para>
			/// </summary>
			public const long SmallestNonZero = 1L;

			/// <summary>
			/// Minimum FP value, but values outside of <see cref="F:Photon.Deterministic.FP.Raw.UseableMin" /> and <see cref="F:Photon.Deterministic.FP.Raw.UseableMax" /> (inclusive) can overflow when multiplied.
			/// <para>Closest double: -140737488355328</para>
			/// </summary>
			public const long MinValue = long.MinValue;

			/// <summary>
			/// Maximum FP value, but values outside of <see cref="F:Photon.Deterministic.FP.Raw.UseableMin" /> and <see cref="F:Photon.Deterministic.FP.Raw.UseableMax" /> (inclusive) can overflow when multiplied.
			/// <para>Closest double: 140737488355328</para>
			/// </summary>
			public const long MaxValue = long.MaxValue;

			/// <summary>
			/// Represents the highest negative FP number that can be multiplied with itself and not cause an overflow (exceeding long range).
			/// <para>Closest double: -32768</para>
			/// </summary>
			public const long UseableMin = -2147483648L;

			/// <summary>
			/// Represents the highest FP number that can be multiplied with itself and not cause an overflow (exceeding long range).
			/// <para>Closest double: 32767.9999847412</para>
			/// </summary>
			public const long UseableMax = 2147483647L;

			/// <summary>
			/// Pi number.
			/// <para>Closest double: 3.14158630371094</para>
			/// </summary>
			public const long Pi = 205887L;

			/// <summary>
			/// 1/Pi.
			/// <para>Closest double: 0.318313598632813</para>
			/// </summary>
			public const long PiInv = 20861L;

			/// <summary>
			/// 2 * Pi.
			/// <para>Closest double: 6.28318786621094</para>
			/// </summary>
			public const long PiTimes2 = 411775L;

			/// <summary>
			/// Pi / 2.
			/// <para>Closest double: 1.57080078125</para>
			/// </summary>
			public const long PiOver2 = 102944L;

			/// <summary>
			/// 2 / Pi.
			/// <para>Closest double: 0.636627197265625</para>
			/// </summary>
			public const long PiOver2Inv = 41722L;

			/// <summary>
			/// Pi / 4.
			/// <para>Closest double: 0.785400390625</para>
			/// </summary>
			public const long PiOver4 = 51472L;

			/// <summary>
			/// 3 * Pi / 4.
			/// <para>Closest double: 2.356201171875</para>
			/// </summary>
			public const long Pi3Over4 = 154416L;

			/// <summary>
			/// 4 * Pi / 3.
			/// <para>Closest double: 4.18879699707031</para>
			/// </summary>
			public const long Pi4Over3 = 274517L;

			/// <summary>
			/// Degrees-to-radians conversion constant.
			/// <para>Closest double: 0.0174560546875</para>
			/// </summary>
			public const long Deg2Rad = 1144L;

			/// <summary>
			/// Radians-to-degrees conversion constant.
			/// <para>Closest double: 57.2957763671875</para>
			/// </summary>
			public const long Rad2Deg = 3754936L;

			/// <summary>
			/// FP constant representing the number 0.
			/// <para>Closest double: 0</para>
			/// </summary>
			public const long _0 = 0L;

			/// <summary>
			/// FP constant representing the number 1.
			/// <para>Closest double: 1</para>
			/// </summary>
			public const long _1 = 65536L;

			/// <summary>
			/// FP constant representing the number 2.
			/// <para>Closest double: 2</para>
			/// </summary>
			public const long _2 = 131072L;

			/// <summary>
			/// FP constant representing the number 3.
			/// <para>Closest double: 3</para>
			/// </summary>
			public const long _3 = 196608L;

			/// <summary>
			/// FP constant representing the number 4.
			/// <para>Closest double: 4</para>
			/// </summary>
			public const long _4 = 262144L;

			/// <summary>
			/// FP constant representing the number 5.
			/// <para>Closest double: 5</para>
			/// </summary>
			public const long _5 = 327680L;

			/// <summary>
			/// FP constant representing the number 6.
			/// <para>Closest double: 6</para>
			/// </summary>
			public const long _6 = 393216L;

			/// <summary>
			/// FP constant representing the number 7.
			/// <para>Closest double: 7</para>
			/// </summary>
			public const long _7 = 458752L;

			/// <summary>
			/// FP constant representing the number 8.
			/// <para>Closest double: 8</para>
			/// </summary>
			public const long _8 = 524288L;

			/// <summary>
			/// FP constant representing the number 9.
			/// <para>Closest double: 9</para>
			/// </summary>
			public const long _9 = 589824L;

			/// <summary>
			/// FP constant representing the number 10.
			/// <para>Closest double: 10</para>
			/// </summary>
			public const long _10 = 655360L;

			/// <summary>
			/// FP constant representing the number 99.
			/// <para>Closest double: 99</para>
			/// </summary>
			public const long _99 = 6488064L;

			/// <summary>
			/// FP constant representing the number 100.
			/// <para>Closest double: 100</para>
			/// </summary>
			public const long _100 = 6553600L;

			/// <summary>
			/// FP constant representing the number 180.
			/// <para>Closest double: 180</para>
			/// </summary>
			public const long _180 = 11796480L;

			/// <summary>
			/// FP constant representing the number 200.
			/// <para>Closest double: 200</para>
			/// </summary>
			public const long _200 = 13107200L;

			/// <summary>
			/// FP constant representing the number 360.
			/// <para>Closest double: 360</para>
			/// </summary>
			public const long _360 = 23592960L;

			/// <summary>
			/// FP constant representing the number 1000.
			/// <para>Closest double: 1000</para>
			/// </summary>
			public const long _1000 = 65536000L;

			/// <summary>
			/// FP constant representing the number 10000.
			/// <para>Closest double: 10000</para>
			/// </summary>
			public const long _10000 = 655360000L;

			/// <summary>
			/// FP constant representing the number 0.01.
			/// <para>Closest double: 0.0099945068359375</para>
			/// </summary>
			public const long _0_01 = 655L;

			/// <summary>
			/// FP constant representing the number 0.02.
			/// <para>Closest double: 0.0200042724609375</para>
			/// </summary>
			public const long _0_02 = 1311L;

			/// <summary>
			/// FP constant representing the number 0.03.
			/// <para>Closest double: 0.029998779296875</para>
			/// </summary>
			public const long _0_03 = 1966L;

			/// <summary>
			/// FP constant representing the number 0.04.
			/// <para>Closest double: 0.0399932861328125</para>
			/// </summary>
			public const long _0_04 = 2621L;

			/// <summary>
			/// FP constant representing the number 0.05.
			/// <para>Closest double: 0.0500030517578125</para>
			/// </summary>
			public const long _0_05 = 3277L;

			/// <summary>
			/// FP constant representing the number 0.10.
			/// <para>Closest double: 0.100006103515625</para>
			/// </summary>
			public const long _0_10 = 6554L;

			/// <summary>
			/// FP constant representing the number 0.20.
			/// <para>Closest double: 0.199996948242188</para>
			/// </summary>
			public const long _0_20 = 13107L;

			/// <summary>
			/// FP constant representing the number 0.25.
			/// <para>Closest double: 0.25</para>
			/// </summary>
			public const long _0_25 = 16384L;

			/// <summary>
			/// FP constant representing the number 0.50.
			/// <para>Closest double: 0.5</para>
			/// </summary>
			public const long _0_50 = 32768L;

			/// <summary>
			/// FP constant representing the number 0.75.
			/// <para>Closest double: 0.75</para>
			/// </summary>
			public const long _0_75 = 49152L;

			/// <summary>
			/// FP constant representing the number 0.33.
			/// <para>Closest double: 0.333328247070313</para>
			/// </summary>
			public const long _0_33 = 21845L;

			/// <summary>
			/// FP constant representing the number 0.99.
			/// <para>Closest double: 0.990005493164063</para>
			/// </summary>
			public const long _0_99 = 64881L;

			/// <summary>
			/// FP constant representing the number -1.
			/// <para>Closest double: -1</para>
			/// </summary>
			public const long Minus_1 = -65536L;

			/// <summary>
			/// FP constant representing 360 degrees in radian.
			/// <para>Closest double: 6.28318786621094</para>
			/// </summary>
			public const long Rad_360 = 411775L;

			/// <summary>
			/// FP constant representing 180 degrees in radian.
			/// <para>Closest double: 3.14158630371094</para>
			/// </summary>
			public const long Rad_180 = 205887L;

			/// <summary>
			/// FP constant representing 90 degrees in radian.
			/// <para>Closest double: 1.57080078125</para>
			/// </summary>
			public const long Rad_90 = 102944L;

			/// <summary>
			/// FP constant representing 45 degrees in radian.
			/// <para>Closest double: 0.785400390625</para>
			/// </summary>
			public const long Rad_45 = 51472L;

			/// <summary>
			/// FP constant representing 22.5 degrees in radian.
			/// <para>Closest double: 0.3927001953125</para>
			/// </summary>
			public const long Rad_22_50 = 25736L;

			/// <summary>
			/// FP constant representing the number 1.01.
			/// <para>Closest double: 1.00999450683594</para>
			/// </summary>
			public const long _1_01 = 66191L;

			/// <summary>
			/// FP constant representing the number 1.02.
			/// <para>Closest double: 1.02000427246094</para>
			/// </summary>
			public const long _1_02 = 66847L;

			/// <summary>
			/// FP constant representing the number 1.03.
			/// <para>Closest double: 1.02999877929688</para>
			/// </summary>
			public const long _1_03 = 67502L;

			/// <summary>
			/// FP constant representing the number 1.04.
			/// <para>Closest double: 1.03999328613281</para>
			/// </summary>
			public const long _1_04 = 68157L;

			/// <summary>
			/// FP constant representing the number 1.05.
			/// <para>Closest double: 1.05000305175781</para>
			/// </summary>
			public const long _1_05 = 68813L;

			/// <summary>
			/// FP constant representing the number 1.10.
			/// <para>Closest double: 1.10000610351563</para>
			/// </summary>
			public const long _1_10 = 72090L;

			/// <summary>
			/// FP constant representing the number 1.20.
			/// <para>Closest double: 1.19999694824219</para>
			/// </summary>
			public const long _1_20 = 78643L;

			/// <summary>
			/// FP constant representing the number 1.25.
			/// <para>Closest double: 1.25</para>
			/// </summary>
			public const long _1_25 = 81920L;

			/// <summary>
			/// FP constant representing the number 1.50.
			/// <para>Closest double: 1.5</para>
			/// </summary>
			public const long _1_50 = 98304L;

			/// <summary>
			/// FP constant representing the number 1.75.
			/// <para>Closest double: 1.75</para>
			/// </summary>
			public const long _1_75 = 114688L;

			/// <summary>
			/// FP constant representing the number 1.33.
			/// <para>Closest double: 1.33332824707031</para>
			/// </summary>
			public const long _1_33 = 87381L;

			/// <summary>
			/// FP constant representing the number 1.99.
			/// <para>Closest double: 1.99000549316406</para>
			/// </summary>
			public const long _1_99 = 130417L;

			/// <summary>
			/// FP constant representing the epsilon value EN1.
			/// <para>Closest double: 0.100006103515625</para>
			/// </summary>
			public const long EN1 = 6554L;

			/// <summary>
			/// FP constant representing the epsilon value EN2.
			/// <para>Closest double: 0.0099945068359375</para>
			/// </summary>
			public const long EN2 = 655L;

			/// <summary>
			/// FP constant representing the epsilon value EN3.
			/// <para>Closest double: 0.001007080078125</para>
			/// </summary>
			public const long EN3 = 66L;

			/// <summary>
			/// FP constant representing the epsilon value EN4.
			/// <para>Closest double: 0.0001068115234375</para>
			/// </summary>
			public const long EN4 = 7L;

			/// <summary>
			/// FP constant representing the epsilon value EN5.
			/// <para>Closest double: 1.52587890625E-05</para>
			/// </summary>
			public const long EN5 = 1L;

			/// <summary>
			/// FP constant representing Epsilon <see cref="F:Photon.Deterministic.FP.Raw.EN3" />.
			/// <para>Closest double: 0.001007080078125</para>
			/// </summary>
			public const long Epsilon = 66L;

			/// <summary>
			/// FP constant representing the Euler Number constant.
			/// <para>Closest double: 2.71827697753906</para>
			/// </summary>
			public const long E = 178145L;

			/// <summary>
			/// FP constant representing Log(E).
			/// <para>Closest double: 1.44268798828125</para>
			/// </summary>
			public const long Log2_E = 94548L;

			/// <summary>
			/// FP constant representing Log(10).
			/// <para>Closest double: 3.32192993164063</para>
			/// </summary>
			public const long Log2_10 = 217706L;
		}

		/// <summary>
		/// Compares <see cref="T:Photon.Deterministic.FP" />s.
		/// </summary>
		public class Comparer : Comparer<FP>
		{
			/// <summary>
			/// A global FP comparer instance.
			/// </summary>
			public static readonly Comparer Instance = new Comparer();

			private Comparer()
			{
			}

			/// <summary>
			/// Compares two instances of FP and returns an integer that indicates whether the first instance is less than, equal to, or greater than the second instance.
			/// </summary>
			/// <param name="x">The first instance to compare.</param>
			/// <param name="y">The second instance to compare.</param>
			/// <returns>
			/// A signed integer that indicates the relative values of x and y, as shown in the following table:
			/// - Less than zero: x is less than y.
			/// - Zero: x equals y.
			/// - Greater than zero: x is greater than y.
			/// </returns>
			public override int Compare(FP x, FP y)
			{
				return x.RawValue.CompareTo(y.RawValue);
			}
		}

		/// <summary>
		/// Equality comparer for <see cref="T:Photon.Deterministic.FP" />s.
		/// </summary>
		public class EqualityComparer : IEqualityComparer<FP>
		{
			/// <summary>
			/// A global FP equality comparer instance.
			/// </summary>
			public static readonly EqualityComparer Instance = new EqualityComparer();

			private EqualityComparer()
			{
			}

			bool IEqualityComparer<FP>.Equals(FP x, FP y)
			{
				return x.RawValue == y.RawValue;
			}

			int IEqualityComparer<FP>.GetHashCode(FP num)
			{
				return num.RawValue.GetHashCode();
			}
		}

		/// <summary>
		/// Represents the size of a variable in bytes.
		/// <para>The SIZE constant is used to determine the size of a variable in bytes.</para>
		/// </summary>
		public const int SIZE = 8;

		internal const int DecimalFractionDigits = 5;

		internal const double DecimalFractionNormalizer = 100000.0;

		private const int FRACTIONS_COUNT = 5;

		/// <summary>
		/// The value of one as a fixed-point number.
		/// </summary>
		public const long RAW_ONE = 65536L;

		/// <summary>
		/// Represents a constant that holds the raw value of zero for the <see cref="T:Photon.Deterministic.FP" /> struct.
		/// </summary>
		public const long RAW_ZERO = 0L;

		/// <summary>
		/// Represents the precision used for Fixed Point calculations.
		/// </summary>
		/// <remarks>
		/// The Precision constant is used to determine the number of decimal places in Fixed Point calculations.
		/// </remarks>
		public const int Precision = 16;

		/// <summary>
		/// The size in bits of the fixed-point number. (64)
		/// </summary>
		public const int Bits = 64;

		/// <summary>
		/// Represents the value of the rounding constant used in Fixed Point multiplication.
		/// </summary>
		public const long MulRound = 32768L;

		/// <summary>
		/// Represents the bit shift used in Fixed Point multiplication.
		/// </summary>
		public const int MulShift = 16;

		public const int MulShiftTrunc = 16;

		internal const bool UsesRoundedConstants = true;

		/// <summary>
		/// The raw integer value of the fixed-point number.
		/// </summary>
		[FieldOffset(0)]
		public long RawValue;

		/// <summary>
		/// The smallest FP unit that is not 0.
		/// <para>Closest double: 1.52587890625E-05</para>
		/// </summary>
		public unsafe static FP SmallestNonZero
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 1L;
				return *(FP*)(&num);
			}
		}

		/// <summary>
		/// Minimum FP value, but values outside of <see cref="P:Photon.Deterministic.FP.UseableMin" /> and <see cref="P:Photon.Deterministic.FP.UseableMax" /> (inclusive) can overflow when multiplied.
		/// <para>Closest double: -140737488355328</para>
		/// </summary>
		public unsafe static FP MinValue
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = long.MinValue;
				return *(FP*)(&num);
			}
		}

		/// <summary>
		/// Maximum FP value, but values outside of <see cref="P:Photon.Deterministic.FP.UseableMin" /> and <see cref="P:Photon.Deterministic.FP.UseableMax" /> (inclusive) can overflow when multiplied.
		/// <para>Closest double: 140737488355328</para>
		/// </summary>
		public unsafe static FP MaxValue
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = long.MaxValue;
				return *(FP*)(&num);
			}
		}

		/// <summary>
		/// Represents the highest negative FP number that can be multiplied with itself and not cause an overflow (exceeding long range).
		/// <para>Closest double: -32768</para>
		/// </summary>
		public unsafe static FP UseableMin
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = -2147483648L;
				return *(FP*)(&num);
			}
		}

		/// <summary>
		/// Represents the highest FP number that can be multiplied with itself and not cause an overflow (exceeding long range).
		/// <para>Closest double: 32767.9999847412</para>
		/// </summary>
		public unsafe static FP UseableMax
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 2147483647L;
				return *(FP*)(&num);
			}
		}

		/// <summary>
		/// Pi number.
		/// <para>Closest double: 3.14158630371094</para>
		/// </summary>
		public unsafe static FP Pi
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 205887L;
				return *(FP*)(&num);
			}
		}

		/// <summary>
		/// 1/Pi.
		/// <para>Closest double: 0.318313598632813</para>
		/// </summary>
		public unsafe static FP PiInv
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 20861L;
				return *(FP*)(&num);
			}
		}

		/// <summary>
		/// 2 * Pi.
		/// <para>Closest double: 6.28318786621094</para>
		/// </summary>
		public unsafe static FP PiTimes2
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 411775L;
				return *(FP*)(&num);
			}
		}

		/// <summary>
		/// Pi / 2.
		/// <para>Closest double: 1.57080078125</para>
		/// </summary>
		public unsafe static FP PiOver2
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 102944L;
				return *(FP*)(&num);
			}
		}

		/// <summary>
		/// 2 / Pi.
		/// <para>Closest double: 0.636627197265625</para>
		/// </summary>
		public unsafe static FP PiOver2Inv
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 41722L;
				return *(FP*)(&num);
			}
		}

		/// <summary>
		/// Pi / 4.
		/// <para>Closest double: 0.785400390625</para>
		/// </summary>
		public unsafe static FP PiOver4
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 51472L;
				return *(FP*)(&num);
			}
		}

		/// <summary>
		/// 3 * Pi / 4.
		/// <para>Closest double: 2.356201171875</para>
		/// </summary>
		public unsafe static FP Pi3Over4
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 154416L;
				return *(FP*)(&num);
			}
		}

		/// <summary>
		/// 4 * Pi / 3.
		/// <para>Closest double: 4.18879699707031</para>
		/// </summary>
		public unsafe static FP Pi4Over3
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 274517L;
				return *(FP*)(&num);
			}
		}

		/// <summary>
		/// Degrees-to-radians conversion constant.
		/// <para>Closest double: 0.0174560546875</para>
		/// </summary>
		public unsafe static FP Deg2Rad
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 1144L;
				return *(FP*)(&num);
			}
		}

		/// <summary>
		/// Radians-to-degrees conversion constant.
		/// <para>Closest double: 57.2957763671875</para>
		/// </summary>
		public unsafe static FP Rad2Deg
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 3754936L;
				return *(FP*)(&num);
			}
		}

		/// <summary>
		/// FP constant representing the number 0.
		/// <para>Closest double: 0</para>
		/// </summary>
		public unsafe static FP _0
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 0L;
				return *(FP*)(&num);
			}
		}

		/// <summary>
		/// FP constant representing the number 1.
		/// <para>Closest double: 1</para>
		/// </summary>
		public unsafe static FP _1
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 65536L;
				return *(FP*)(&num);
			}
		}

		/// <summary>
		/// FP constant representing the number 2.
		/// <para>Closest double: 2</para>
		/// </summary>
		public unsafe static FP _2
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 131072L;
				return *(FP*)(&num);
			}
		}

		/// <summary>
		/// FP constant representing the number 3.
		/// <para>Closest double: 3</para>
		/// </summary>
		public unsafe static FP _3
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 196608L;
				return *(FP*)(&num);
			}
		}

		/// <summary>
		/// FP constant representing the number 4.
		/// <para>Closest double: 4</para>
		/// </summary>
		public unsafe static FP _4
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 262144L;
				return *(FP*)(&num);
			}
		}

		/// <summary>
		/// FP constant representing the number 5.
		/// <para>Closest double: 5</para>
		/// </summary>
		public unsafe static FP _5
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 327680L;
				return *(FP*)(&num);
			}
		}

		/// <summary>
		/// FP constant representing the number 6.
		/// <para>Closest double: 6</para>
		/// </summary>
		public unsafe static FP _6
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 393216L;
				return *(FP*)(&num);
			}
		}

		/// <summary>
		/// FP constant representing the number 7.
		/// <para>Closest double: 7</para>
		/// </summary>
		public unsafe static FP _7
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 458752L;
				return *(FP*)(&num);
			}
		}

		/// <summary>
		/// FP constant representing the number 8.
		/// <para>Closest double: 8</para>
		/// </summary>
		public unsafe static FP _8
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 524288L;
				return *(FP*)(&num);
			}
		}

		/// <summary>
		/// FP constant representing the number 9.
		/// <para>Closest double: 9</para>
		/// </summary>
		public unsafe static FP _9
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 589824L;
				return *(FP*)(&num);
			}
		}

		/// <summary>
		/// FP constant representing the number 10.
		/// <para>Closest double: 10</para>
		/// </summary>
		public unsafe static FP _10
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 655360L;
				return *(FP*)(&num);
			}
		}

		/// <summary>
		/// FP constant representing the number 99.
		/// <para>Closest double: 99</para>
		/// </summary>
		public unsafe static FP _99
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 6488064L;
				return *(FP*)(&num);
			}
		}

		/// <summary>
		/// FP constant representing the number 100.
		/// <para>Closest double: 100</para>
		/// </summary>
		public unsafe static FP _100
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 6553600L;
				return *(FP*)(&num);
			}
		}

		/// <summary>
		/// FP constant representing the number 180.
		/// <para>Closest double: 180</para>
		/// </summary>
		public unsafe static FP _180
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 11796480L;
				return *(FP*)(&num);
			}
		}

		/// <summary>
		/// FP constant representing the number 200.
		/// <para>Closest double: 200</para>
		/// </summary>
		public unsafe static FP _200
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 13107200L;
				return *(FP*)(&num);
			}
		}

		/// <summary>
		/// FP constant representing the number 360.
		/// <para>Closest double: 360</para>
		/// </summary>
		public unsafe static FP _360
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 23592960L;
				return *(FP*)(&num);
			}
		}

		/// <summary>
		/// FP constant representing the number 1000.
		/// <para>Closest double: 1000</para>
		/// </summary>
		public unsafe static FP _1000
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 65536000L;
				return *(FP*)(&num);
			}
		}

		/// <summary>
		/// FP constant representing the number 10000.
		/// <para>Closest double: 10000</para>
		/// </summary>
		public unsafe static FP _10000
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 655360000L;
				return *(FP*)(&num);
			}
		}

		/// <summary>
		/// FP constant representing the number 0.01.
		/// <para>Closest double: 0.0099945068359375</para>
		/// </summary>
		public unsafe static FP _0_01
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 655L;
				return *(FP*)(&num);
			}
		}

		/// <summary>
		/// FP constant representing the number 0.02.
		/// <para>Closest double: 0.0200042724609375</para>
		/// </summary>
		public unsafe static FP _0_02
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 1311L;
				return *(FP*)(&num);
			}
		}

		/// <summary>
		/// FP constant representing the number 0.03.
		/// <para>Closest double: 0.029998779296875</para>
		/// </summary>
		public unsafe static FP _0_03
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 1966L;
				return *(FP*)(&num);
			}
		}

		/// <summary>
		/// FP constant representing the number 0.04.
		/// <para>Closest double: 0.0399932861328125</para>
		/// </summary>
		public unsafe static FP _0_04
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 2621L;
				return *(FP*)(&num);
			}
		}

		/// <summary>
		/// FP constant representing the number 0.05.
		/// <para>Closest double: 0.0500030517578125</para>
		/// </summary>
		public unsafe static FP _0_05
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 3277L;
				return *(FP*)(&num);
			}
		}

		/// <summary>
		/// FP constant representing the number 0.10.
		/// <para>Closest double: 0.100006103515625</para>
		/// </summary>
		public unsafe static FP _0_10
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 6554L;
				return *(FP*)(&num);
			}
		}

		/// <summary>
		/// FP constant representing the number 0.20.
		/// <para>Closest double: 0.199996948242188</para>
		/// </summary>
		public unsafe static FP _0_20
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 13107L;
				return *(FP*)(&num);
			}
		}

		/// <summary>
		/// FP constant representing the number 0.25.
		/// <para>Closest double: 0.25</para>
		/// </summary>
		public unsafe static FP _0_25
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 16384L;
				return *(FP*)(&num);
			}
		}

		/// <summary>
		/// FP constant representing the number 0.50.
		/// <para>Closest double: 0.5</para>
		/// </summary>
		public unsafe static FP _0_50
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 32768L;
				return *(FP*)(&num);
			}
		}

		/// <summary>
		/// FP constant representing the number 0.75.
		/// <para>Closest double: 0.75</para>
		/// </summary>
		public unsafe static FP _0_75
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 49152L;
				return *(FP*)(&num);
			}
		}

		/// <summary>
		/// FP constant representing the number 0.33.
		/// <para>Closest double: 0.333328247070313</para>
		/// </summary>
		public unsafe static FP _0_33
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 21845L;
				return *(FP*)(&num);
			}
		}

		/// <summary>
		/// FP constant representing the number 0.99.
		/// <para>Closest double: 0.990005493164063</para>
		/// </summary>
		public unsafe static FP _0_99
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 64881L;
				return *(FP*)(&num);
			}
		}

		/// <summary>
		/// FP constant representing the number -1.
		/// <para>Closest double: -1</para>
		/// </summary>
		public unsafe static FP Minus_1
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = -65536L;
				return *(FP*)(&num);
			}
		}

		/// <summary>
		/// FP constant representing 360 degrees in radian.
		/// <para>Closest double: 6.28318786621094</para>
		/// </summary>
		public unsafe static FP Rad_360
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 411775L;
				return *(FP*)(&num);
			}
		}

		/// <summary>
		/// FP constant representing 180 degrees in radian.
		/// <para>Closest double: 3.14158630371094</para>
		/// </summary>
		public unsafe static FP Rad_180
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 205887L;
				return *(FP*)(&num);
			}
		}

		/// <summary>
		/// FP constant representing 90 degrees in radian.
		/// <para>Closest double: 1.57080078125</para>
		/// </summary>
		public unsafe static FP Rad_90
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 102944L;
				return *(FP*)(&num);
			}
		}

		/// <summary>
		/// FP constant representing 45 degrees in radian.
		/// <para>Closest double: 0.785400390625</para>
		/// </summary>
		public unsafe static FP Rad_45
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 51472L;
				return *(FP*)(&num);
			}
		}

		/// <summary>
		/// FP constant representing 22.5 degrees in radian.
		/// <para>Closest double: 0.3927001953125</para>
		/// </summary>
		public unsafe static FP Rad_22_50
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 25736L;
				return *(FP*)(&num);
			}
		}

		/// <summary>
		/// FP constant representing the number 1.01.
		/// <para>Closest double: 1.00999450683594</para>
		/// </summary>
		public unsafe static FP _1_01
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 66191L;
				return *(FP*)(&num);
			}
		}

		/// <summary>
		/// FP constant representing the number 1.02.
		/// <para>Closest double: 1.02000427246094</para>
		/// </summary>
		public unsafe static FP _1_02
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 66847L;
				return *(FP*)(&num);
			}
		}

		/// <summary>
		/// FP constant representing the number 1.03.
		/// <para>Closest double: 1.02999877929688</para>
		/// </summary>
		public unsafe static FP _1_03
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 67502L;
				return *(FP*)(&num);
			}
		}

		/// <summary>
		/// FP constant representing the number 1.04.
		/// <para>Closest double: 1.03999328613281</para>
		/// </summary>
		public unsafe static FP _1_04
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 68157L;
				return *(FP*)(&num);
			}
		}

		/// <summary>
		/// FP constant representing the number 1.05.
		/// <para>Closest double: 1.05000305175781</para>
		/// </summary>
		public unsafe static FP _1_05
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 68813L;
				return *(FP*)(&num);
			}
		}

		/// <summary>
		/// FP constant representing the number 1.10.
		/// <para>Closest double: 1.10000610351563</para>
		/// </summary>
		public unsafe static FP _1_10
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 72090L;
				return *(FP*)(&num);
			}
		}

		/// <summary>
		/// FP constant representing the number 1.20.
		/// <para>Closest double: 1.19999694824219</para>
		/// </summary>
		public unsafe static FP _1_20
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 78643L;
				return *(FP*)(&num);
			}
		}

		/// <summary>
		/// FP constant representing the number 1.25.
		/// <para>Closest double: 1.25</para>
		/// </summary>
		public unsafe static FP _1_25
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 81920L;
				return *(FP*)(&num);
			}
		}

		/// <summary>
		/// FP constant representing the number 1.50.
		/// <para>Closest double: 1.5</para>
		/// </summary>
		public unsafe static FP _1_50
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 98304L;
				return *(FP*)(&num);
			}
		}

		/// <summary>
		/// FP constant representing the number 1.75.
		/// <para>Closest double: 1.75</para>
		/// </summary>
		public unsafe static FP _1_75
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 114688L;
				return *(FP*)(&num);
			}
		}

		/// <summary>
		/// FP constant representing the number 1.33.
		/// <para>Closest double: 1.33332824707031</para>
		/// </summary>
		public unsafe static FP _1_33
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 87381L;
				return *(FP*)(&num);
			}
		}

		/// <summary>
		/// FP constant representing the number 1.99.
		/// <para>Closest double: 1.99000549316406</para>
		/// </summary>
		public unsafe static FP _1_99
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 130417L;
				return *(FP*)(&num);
			}
		}

		/// <summary>
		/// FP constant representing the epsilon value EN1.
		/// <para>Closest double: 0.100006103515625</para>
		/// </summary>
		public unsafe static FP EN1
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 6554L;
				return *(FP*)(&num);
			}
		}

		/// <summary>
		/// FP constant representing the epsilon value EN2.
		/// <para>Closest double: 0.0099945068359375</para>
		/// </summary>
		public unsafe static FP EN2
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 655L;
				return *(FP*)(&num);
			}
		}

		/// <summary>
		/// FP constant representing the epsilon value EN3.
		/// <para>Closest double: 0.001007080078125</para>
		/// </summary>
		public unsafe static FP EN3
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 66L;
				return *(FP*)(&num);
			}
		}

		/// <summary>
		/// FP constant representing the epsilon value EN4.
		/// <para>Closest double: 0.0001068115234375</para>
		/// </summary>
		public unsafe static FP EN4
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 7L;
				return *(FP*)(&num);
			}
		}

		/// <summary>
		/// FP constant representing the epsilon value EN5.
		/// <para>Closest double: 1.52587890625E-05</para>
		/// </summary>
		public unsafe static FP EN5
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 1L;
				return *(FP*)(&num);
			}
		}

		/// <summary>
		/// FP constant representing Epsilon <see cref="P:Photon.Deterministic.FP.EN3" />.
		/// <para>Closest double: 0.001007080078125</para>
		/// </summary>
		public unsafe static FP Epsilon
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 66L;
				return *(FP*)(&num);
			}
		}

		/// <summary>
		/// FP constant representing the Euler Number constant.
		/// <para>Closest double: 2.71827697753906</para>
		/// </summary>
		public unsafe static FP E
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 178145L;
				return *(FP*)(&num);
			}
		}

		/// <summary>
		/// FP constant representing Log(E).
		/// <para>Closest double: 1.44268798828125</para>
		/// </summary>
		public unsafe static FP Log2_E
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 94548L;
				return *(FP*)(&num);
			}
		}

		/// <summary>
		/// FP constant representing Log(10).
		/// <para>Closest double: 3.32192993164063</para>
		/// </summary>
		public unsafe static FP Log2_10
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 217706L;
				return *(FP*)(&num);
			}
		}

		/// <summary>
		/// Returns integral part as long.
		/// </summary>
		public readonly long AsLong
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				return RawValue >> 16;
			}
		}

		/// <summary>
		/// Return integral part as int.
		/// </summary>
		public readonly int AsInt
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				return (int)(RawValue >> 16);
			}
		}

		/// <summary>
		/// Return integral part as int.
		/// </summary>
		public readonly short AsShort
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				return (short)(RawValue >> 16);
			}
		}

		/// <summary>
		/// Converts to float.
		/// </summary>
		public readonly float AsFloat
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				return (float)RawValue / 65536f;
			}
		}

		/// <summary>
		/// Converts to double. The returned value is not exact, but rather the one that has the least
		/// significant digits given FP's precision.
		/// </summary>
		public readonly double AsRoundedDouble
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				(int, ulong, uint) decimalParts = GetDecimalParts();
				return (double)decimalParts.Item1 * ((double)decimalParts.Item2 + (double)decimalParts.Item3 / 100000.0);
			}
		}

		/// <summary>
		/// Converts to double.
		/// </summary>
		public readonly double AsDouble
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				return (double)RawValue / 65536.0;
			}
		}

		/// <summary>
		/// Serializes the given pointer using the provided serializer.
		/// </summary>
		/// <param name="ptr">The pointer to the FP object to be serialized.</param>
		/// <param name="serializer">The serializer used for serialization.</param>
		public unsafe static void Serialize(void* ptr, IDeterministicFrameSerializer serializer)
		{
			serializer.Stream.Serialize(&((FP*)ptr)->RawValue);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal FP(long v)
		{
			RawValue = v;
		}

		/// <summary>
		/// Compares this instance of FP to another instance and returns an integer that indicates whether this instance is less than, equal to, or greater than the other instance.
		/// </summary>
		/// <param name="other">The other instance to compare.</param>
		/// <returns>A signed integer that indicates the relative values of this instance and the other instance.</returns>
		public readonly int CompareTo(FP other)
		{
			return RawValue.CompareTo(other.RawValue);
		}

		/// <summary>
		/// Determines whether the current instance is equal to another instance of FP.
		/// </summary>
		/// <param name="other">The instance to compare with the current instance.</param>
		/// <returns><see langword="true" /> if the current instance is equal to the other instance; otherwise, <see langword="false" />.</returns>
		public readonly bool Equals(FP other)
		{
			return RawValue == other.RawValue;
		}

		/// <summary>
		/// Determines whether the current instance of FP is equal to the specified object.
		/// </summary>
		/// <param name="obj">The object to compare with the current instance of FP.</param>
		/// <returns>
		/// <see langword="true" /> if the specified object is equal to the current instance of FP; otherwise, <see langword="false" />.
		/// </returns>
		public override readonly bool Equals(object obj)
		{
			if (obj is FP)
			{
				return RawValue == ((FP)obj).RawValue;
			}
			return false;
		}

		/// <summary>
		/// Computes the hash code for the current instance of the FP struct.
		/// </summary>
		/// <returns>
		/// A 32-bit signed integer hash code.
		/// </returns>
		public override readonly int GetHashCode()
		{
			return RawValue.GetHashCode();
		}

		/// <summary>
		/// Returns a string representation of the current FP value.
		/// </summary>
		/// <returns>
		/// A string representation of the current FP value.
		/// </returns>
		public override readonly string ToString()
		{
			var (num, value, num2) = GetDecimalParts();
			if (num2 == 0)
			{
				return (RawValue >> 16).ToString();
			}
			StringBuilder stringBuilder = new StringBuilder();
			if (num < 0)
			{
				stringBuilder.Append('-');
			}
			stringBuilder.Append(value);
			stringBuilder.Append('.');
			if (num2 < 10000)
			{
				stringBuilder.Append('0');
				if (num2 < 1000)
				{
					stringBuilder.Append('0');
					if (num2 < 100)
					{
						stringBuilder.Append('0');
						if (num2 < 10)
						{
							stringBuilder.Append('0');
						}
					}
				}
			}
			if (num2 % 10000 == 0)
			{
				num2 /= 10000;
			}
			else if (num2 % 1000 == 0)
			{
				num2 /= 1000;
			}
			else if (num2 % 100 == 0)
			{
				num2 /= 100;
			}
			else if (num2 % 10 == 0)
			{
				num2 /= 10;
			}
			stringBuilder.Append(num2);
			return stringBuilder.ToString();
		}

		/// <summary>
		/// Converts the value of the current FP object to its equivalent string representation using the legacy format.
		/// </summary>
		/// <returns>
		/// The string representation of the value of the current FP object, formatted using the legacy format.
		/// </returns>
		[Obsolete]
		public string ToStringLegacy()
		{
			return AsFloat.ToString(CultureInfo.InvariantCulture);
		}

		/// <summary>
		/// Returns a string that represents the <see cref="T:Photon.Deterministic.FP" />.
		/// </summary>
		/// <returns>String representation of the FP.</returns>
		public readonly string ToString(string format)
		{
			return AsDouble.ToString(format, CultureInfo.InvariantCulture);
		}

		/// <summary>
		/// Returns a string that represents the <see cref="T:Photon.Deterministic.FP" /> using a custom format.
		/// </summary>
		/// <returns>String representation of the FP.</returns>
		public readonly string ToStringInternal()
		{
			long num = Math.Abs(RawValue);
			string text = $"{num >> 16}.{(num % 65536).ToString(CultureInfo.InvariantCulture).PadLeft(5, '0')}";
			if (RawValue < 0)
			{
				return "-" + text;
			}
			return text;
		}

		/// <summary>
		/// Converts a double value to an instance of the FP, with rounding to the nearest representable FP.
		/// </summary>
		/// <param name="value">The rounded double value to convert.</param>
		/// <returns>The FP value that represents the rounded double value.</returns>
		public static FP FromRoundedDouble_UNSAFE(double value)
		{
			return new FP((long)Math.Round(value * 65536.0));
		}

		/// <summary>
		/// Converts a double value to an instance of the FP, with rounding towards zero..
		/// To round towards nearest representable FP, use <see cref="M:Photon.Deterministic.FP.FromRoundedDouble_UNSAFE(System.Double)" />.
		/// This method is marked as unsafe because it is not deterministic.
		/// </summary>
		/// <param name="value">The double value to convert.</param>
		/// <returns>An instance of the FP struct that represents the converted value.</returns>
		public static FP FromDouble_UNSAFE(double value)
		{
			return new FP((long)(value * 65536.0));
		}

		/// <summary>
		/// Converts a single-precision floating-point value to an instance of the FP, with rounding to the nearest representable FP.
		/// This method is marked as unsafe because it is not deterministic.
		/// </summary>
		/// <param name="value">The value to convert.</param>
		/// <returns>The converted value.</returns>
		public static FP FromRoundedFloat_UNSAFE(float value)
		{
			return new FP((long)Math.Round(value * 65536f));
		}

		/// <summary>
		/// Converts a single-precision floating-point value to an instance of the FP, with rounding towards zero..
		/// To round towards nearest representable FP, use <see cref="M:Photon.Deterministic.FP.FromRoundedFloat_UNSAFE(System.Single)" />.
		/// This method is marked as unsafe because it is not deterministic.
		/// </summary>
		/// <param name="value">The value to convert.</param>
		/// <returns>The converted value.</returns>
		public static FP FromFloat_UNSAFE(float value)
		{
			return new FP(checked((long)(value * 65536f)));
		}

		/// <summary>
		/// Converts a raw integer value to an instance of FP.
		/// </summary>
		/// <param name="value">The raw integer value to convert.</param>
		/// <returns>A new instance of FP that represents the same value as the raw integer.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static FP FromRaw(long value)
		{
			FP result = default(FP);
			result.RawValue = value;
			return result;
		}

		/// <summary>
		/// Creates an instance of FP from a string representation of a float value.
		/// This method is marked as unsafe because it is not deterministic.
		/// </summary>
		/// <param name="value">The string representation of the float value.</param>
		/// <returns>An instance of FP representing the float value.</returns>
		[Obsolete("Use FromString instead.")]
		public static FP FromString_UNSAFE(string value)
		{
			return FromFloat_UNSAFE((float)double.Parse(value, CultureInfo.InvariantCulture));
		}

		/// <summary>
		/// Converts a string representation of a fixed-point number to an instance of the FP struct.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		/// <exception cref="T:System.FormatException"></exception>
		public static FP FromString(string value)
		{
			if (value == null)
			{
				return _0;
			}
			value = value.Trim();
			if (value.Length == 0)
			{
				return _0;
			}
			bool flag = false;
			if (flag = value[0] == '-')
			{
				value = value.Substring(1);
			}
			bool flag2 = value[0] == '.';
			string[] array = value.Split(new char[1] { '.' }, StringSplitOptions.RemoveEmptyEntries);
			long num = 0L;
			num = array.Length switch
			{
				1 => (!flag2) ? ParseInteger(array[0]) : ParseFractions(array[0]), 
				2 => checked(ParseInteger(array[0]) + ParseFractions(array[1])), 
				_ => throw new FormatException(value), 
			};
			if (flag)
			{
				return new FP(-num);
			}
			return new FP(num);
		}

		private static long ParseInteger(string format)
		{
			return long.Parse(format) * 65536;
		}

		private static long ParseFractions(string format)
		{
			long num;
			switch (format.Length)
			{
			case 0:
				return 0L;
			case 1:
				num = 10L;
				break;
			case 2:
				num = 100L;
				break;
			case 3:
				num = 1000L;
				break;
			case 4:
				num = 10000L;
				break;
			case 5:
				num = 100000L;
				break;
			case 6:
				num = 1000000L;
				break;
			case 7:
				num = 10000000L;
				break;
			default:
			{
				if (format.Length > 14)
				{
					format = format.Substring(0, 14);
				}
				num = 100000000L;
				for (int i = 8; i < format.Length; i++)
				{
					num *= 10;
				}
				break;
			}
			}
			long num2 = long.Parse(format);
			return (num2 * 65536 + num / 2) / num;
		}

		internal static long RawMultiply(FP x, FP y)
		{
			return x.RawValue * y.RawValue + 32768 >> 16;
		}

		internal static long RawMultiply(FP x, FP y, FP z)
		{
			y.RawValue = x.RawValue * y.RawValue + 32768 >> 16;
			return y.RawValue * z.RawValue + 32768 >> 16;
		}

		internal static long RawMultiply(FP x, FP y, FP z, FP a)
		{
			y.RawValue = x.RawValue * y.RawValue + 32768 >> 16;
			z.RawValue = y.RawValue * z.RawValue + 32768 >> 16;
			return z.RawValue * a.RawValue + 32768 >> 16;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static FP MulTruncate(FP x, FP y)
		{
			return FromRaw(x.RawValue * y.RawValue >> 16);
		}

		internal readonly (int Sign, ulong Integer, uint Fraction) GetDecimalParts()
		{
			int item;
			ulong num;
			if (RawValue < 0)
			{
				item = -1;
				num = (ulong)(-RawValue);
			}
			else
			{
				item = 1;
				num = (ulong)RawValue;
			}
			uint num2 = (uint)(num & 0xFFFF);
			ulong item2 = num >> 16;
			if (num2 == 0)
			{
				return (Sign: item, Integer: item2, Fraction: 0u);
			}
			uint num3 = (uint)((ulong)((long)((num2 << 1) - 1) * 762939453125L) / 10000000000uL);
			uint num4 = (uint)((ulong)((long)((num2 << 1) + 1) * 762939453125L) / 10000000000uL);
			uint num5 = num3 + num4 >> 1;
			uint num6 = 0u;
			uint num7 = num3 / 10000;
			uint num8 = num4 / 10000;
			if (num7 == num8)
			{
				uint num9 = num3 / 1000;
				uint num10 = num4 / 1000;
				num6 = ((num9 != num10) ? ((num5 + 500) / 1000 * 10) : ((num5 + 50) / 100));
			}
			else
			{
				uint num11 = num3 / 100000;
				uint num12 = num4 / 100000;
				if (num11 == num12)
				{
					num6 = (num5 + 5000) / 10000 * 100;
				}
				else
				{
					uint num13 = num3 / 1000000;
					uint num14 = num4 / 1000000;
					num6 = ((num13 != num14) ? ((num5 + 500000) / 1000000 * 10000) : ((num5 + 50000) / 100000 * 1000));
				}
			}
			return (Sign: item, Integer: item2, Fraction: num6);
		}

		/// <summary>
		/// Negates the value.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static FP operator -(FP a)
		{
			a.RawValue = -a.RawValue;
			return a;
		}

		/// FP.Operators.cs
		/// <summary>
		/// Converts the value to its absolute version.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static FP operator +(FP a)
		{
			a.RawValue = a.RawValue;
			return a;
		}

		/// <summary>
		/// Represents the operator to add two FP values.
		/// </summary>
		/// <param name="a">The first FP value.</param>
		/// <param name="b">The second FP value.</param>
		/// <returns>The sum of the two FP values.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static FP operator +(FP a, FP b)
		{
			a.RawValue += b.RawValue;
			return a;
		}

		/// <summary>
		/// Overloads the addition operator to add an integer value to an FP value.
		/// </summary>
		/// <param name="a">The FP value.</param>
		/// <param name="b">The integer value to add.</param>
		/// <returns>The result of adding the integer value to the FP value.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static FP operator +(FP a, int b)
		{
			a.RawValue += (long)b << 16;
			return a;
		}

		/// <summary>
		/// Represents the operator overloading for adding an integer and an FP value.
		/// </summary>
		/// <param name="a">The integer value.</param>
		/// <param name="b">The FP value.</param>
		/// <returns>The result of adding the integer and FP values.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static FP operator +(int a, FP b)
		{
			b.RawValue = ((long)a << 16) + b.RawValue;
			return b;
		}

		/// <summary>
		/// Subtracts two FP (fixed point) values.
		/// </summary>
		/// <param name="a">The first FP value.</param>
		/// <param name="b">The second FP value.</param>
		/// <returns>The result of subtracting the second FP value from the first FP value.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static FP operator -(FP a, FP b)
		{
			a.RawValue -= b.RawValue;
			return a;
		}

		/// <summary>
		/// Subtracts an integer value from an FP value.
		/// </summary>
		/// <param name="a">The FP value.</param>
		/// <param name="b">The integer value.</param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static FP operator -(FP a, int b)
		{
			a.RawValue -= (long)b << 16;
			return a;
		}

		/// <summary>
		/// Represents an overloaded operator for negating the value of an integer.
		/// </summary>
		/// <param name="a">The integer value to be negated.</param>
		/// <param name="b">The FP value to subtract from.</param>
		/// <returns>The result of subtracting the FP value from the negated integer value.</returns>
		/// <remarks>
		/// This operator subtracts the FP value from the negated integer value by shifting the integer value to the left by the precision of FP,
		/// then subtracting the raw value of FP from it. The result is then returned as a new FP value.
		/// </remarks>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static FP operator -(int a, FP b)
		{
			b.RawValue = ((long)a << 16) - b.RawValue;
			return b;
		}

		/// <summary>
		/// Represents the operator to multiply two FP values.
		/// </summary>
		/// <param name="a">The first FP value.</param>
		/// <param name="b">The Second FP value</param>
		/// <returns>The product.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static FP operator *(FP a, FP b)
		{
			a.RawValue = a.RawValue * b.RawValue + 32768 >> 16;
			return a;
		}

		/// <summary>
		/// Represents the operator for multiplying a floating-point value by an integer value.
		/// </summary>
		/// <param name="a">The floating-point value.</param>
		/// <param name="b">The integer value.</param>
		/// <returns>The result of multiplying the floating-point value by the integer value.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static FP operator *(FP a, int b)
		{
			a.RawValue *= b;
			return a;
		}

		/// <summary>
		/// Multiplies an integer value by an FP value.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static FP operator *(int a, FP b)
		{
			b.RawValue = a * b.RawValue;
			return b;
		}

		/// <summary>
		/// Represents an operator to perform division on two FP (fixed point) numbers.
		/// </summary>
		/// <param name="a">The dividend.</param>
		/// <param name="b">The divisor.</param>
		/// <returns>The result of the division operation.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static FP operator /(FP a, FP b)
		{
			a.RawValue = (a.RawValue << 16) / b.RawValue;
			return a;
		}

		/// <summary>
		/// Divides an FP value by an integer value.
		/// </summary>
		/// <param name="a">The first FP value.</param>
		/// <param name="b">The second Int32 value.</param>
		/// <returns>The divided result.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static FP operator /(FP a, int b)
		{
			a.RawValue /= b;
			return a;
		}

		/// <summary>
		/// This operator takes an integer value (`a`) and an `FP` value (`b`) and returns the result of the division of `a` by `b`.
		/// </summary>
		/// <param name="a">The integer value to be divided.</param>
		/// <param name="b">The `FP` value to divide by.</param>
		/// <returns>An `FP` value representing the result of the division of `a` by `b`.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static FP operator /(int a, FP b)
		{
			b.RawValue = ((long)a << 32) / b.RawValue;
			return b;
		}

		/// <summary>
		/// Divides an FP value by a high precision divisor.
		/// </summary>
		/// <param name="a">The FP value.</param>
		/// <param name="b">The HighPrecisionDivisor.</param>
		/// <returns>The result.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static FP operator /(FP a, FPHighPrecisionDivisor b)
		{
			a.RawValue = FPHighPrecisionDivisor.RawDiv(a.RawValue, b.RawValue);
			return a;
		}

		/// <summary>
		/// Modulo operator for FP values.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static FP operator %(FP a, FP b)
		{
			a.RawValue %= b.RawValue;
			return a;
		}

		/// <summary>
		/// Modulo operator for FP and integer values.
		/// </summary>
		/// <param name="a">The FP value.</param>
		/// <param name="b">The integer value.</param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static FP operator %(FP a, int b)
		{
			a.RawValue %= (long)b << 16;
			return a;
		}

		/// <summary>
		/// Modulo operator for integer and FP values.
		/// </summary>
		/// <param name="a">The integer value.</param>
		/// <param name="b">The FP value.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static FP operator %(int a, FP b)
		{
			b.RawValue = ((long)a << 16) % b.RawValue;
			return b;
		}

		/// <summary>
		/// Modulo operator for FP and high precision divisor.
		/// </summary>
		/// <param name="a">The FP value.</param>
		/// <param name="b">The high precision divisor.</param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static FP operator %(FP a, FPHighPrecisionDivisor b)
		{
			a.RawValue = FPHighPrecisionDivisor.RawMod(a.RawValue, b.RawValue);
			return a;
		}

		/// <summary>
		/// Represents the operator to compare two FP values.
		/// </summary>
		/// <param name="a">The first FP value.</param>
		/// <param name="b">The second FP value.</param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator <(FP a, FP b)
		{
			return a.RawValue < b.RawValue;
		}

		/// <summary>
		/// Represents the operator to compare an FP value with an integer value.
		/// </summary>
		/// <param name="a">The FP value.</param>
		/// <param name="b">The integer value.</param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator <(FP a, int b)
		{
			return a.RawValue < (long)b << 16;
		}

		/// <summary>
		/// Represents the operator to compare an integer value with an FP value.
		/// </summary>
		/// <param name="a">The integer value.</param>
		/// <param name="b">The FP value.</param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator <(int a, FP b)
		{
			return (long)a << 16 < b.RawValue;
		}

		/// <summary>
		/// Represents the operator to compare two FP values.
		/// </summary>
		/// <param name="a">The first FP value.</param>
		/// <param name="b">The second FP value.</param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator <=(FP a, FP b)
		{
			return a.RawValue <= b.RawValue;
		}

		/// <summary>
		/// Code that defines the operator for less than or equal to comparison between a FP (Fixed Point) value and an integer value.
		/// </summary>
		/// <param name="a">The FP value to compare</param>
		/// <param name="b">The integer value to compare</param>
		/// <returns>Returns <see langword="true" /> if the FP value is less than or equal to the integer value, otherwise <see langword="false" /></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator <=(FP a, int b)
		{
			return a.RawValue <= (long)b << 16;
		}

		/// <summary>
		/// Code that defines the operator for less than or equal to comparison between an integer value and a FP (Fixed Point) value.
		/// </summary>
		/// <param name="a">The integer value to compare</param>
		/// <param name="b">The FP value to compare</param>
		/// <returns>Returns <see langword="true" /> if the integer value is less than or equal to the FP value, otherwise <see langword="false" /></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator <=(int a, FP b)
		{
			return (long)a << 16 <= b.RawValue;
		}

		/// <summary>
		/// Represents the operator to compare two FP values.
		/// </summary>
		/// <param name="a">The first FP value.</param>
		/// <param name="b">The second FP value.</param>
		/// <returns><see langword="true" /> if the first value is greater than the second, otherwise <see langword="false" />.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator >(FP a, FP b)
		{
			return a.RawValue > b.RawValue;
		}

		/// <summary>
		/// Represents the operator to compare an FP value with an integer value.
		/// </summary>
		/// <param name="a">The FP value.</param>
		/// <param name="b">The integer value.</param>
		/// <returns><see langword="true" /> if the FP value is greater than the integer value, otherwise <see langword="false" />.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator >(FP a, int b)
		{
			return a.RawValue > (long)b << 16;
		}

		/// <summary>
		/// Represents the operator to compare an integer value with an FP value.
		/// </summary>
		/// <param name="a">The integer value.</param>
		/// <param name="b">The FP value.</param>
		/// <returns><see langword="true" /> if the integer value is greater than the FP value, otherwise <see langword="false" />.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator >(int a, FP b)
		{
			return (long)a << 16 > b.RawValue;
		}

		/// <summary>
		/// Represents the operator to compare two FP values.
		/// </summary>
		/// <param name="a">The first FP value.</param>
		/// <param name="b">The second FP value.</param>
		/// <returns><see langword="true" /> if the first value is greater than or equal to the second, otherwise <see langword="false" />.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator >=(FP a, FP b)
		{
			return a.RawValue >= b.RawValue;
		}

		/// <summary>
		/// Represents the operator to compare an FP value with an integer value.
		/// </summary>
		/// <param name="a">The FP value.</param>
		/// <param name="b">The integer value.</param>
		/// <returns><see langword="true" /> if the FP value is greater than or equal to the integer value, otherwise <see langword="false" />.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator >=(FP a, int b)
		{
			return a.RawValue >= (long)b << 16;
		}

		/// <summary>
		/// Represents the operator to compare an integer value with an FP value.
		/// </summary>
		/// <param name="a">The integer value.</param>
		/// <param name="b">The FP value.</param>
		/// <returns><see langword="true" /> if the integer value is greater than or equal to the FP value, otherwise <see langword="false" />.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator >=(int a, FP b)
		{
			return (long)a << 16 >= b.RawValue;
		}

		/// <summary>
		/// Compares two FP values for equality.
		/// </summary>
		/// <param name="a">The first FP value.</param>
		/// <param name="b">The second FP value.</param>
		/// <returns><see langword="true" /> if the two values are equal, otherwise <see langword="false" />.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator ==(FP a, FP b)
		{
			return a.RawValue == b.RawValue;
		}

		/// <summary>
		/// Compares an FP value with an integer value for equality.
		/// </summary>
		/// <param name="a">The FP value.</param>
		/// <param name="b">The integer value.</param>
		/// <returns><see langword="true" /> if the two values are equal, otherwise <see langword="false" />.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator ==(FP a, int b)
		{
			return a.RawValue == (long)b << 16;
		}

		/// <summary>
		/// Compares an integer value with an FP value for equality.
		/// </summary>
		/// <param name="a">The integer value.</param>
		/// <param name="b">The FP value.</param>
		/// <returns><see langword="true" /> if the two values are equal, otherwise <see langword="false" />.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator ==(int a, FP b)
		{
			return (long)a << 16 == b.RawValue;
		}

		/// <summary>
		/// Compares two FP values for inequality.
		/// </summary>
		/// <param name="a">The first FP value.</param>
		/// <param name="b">The second FP value.</param>
		/// <returns><see langword="true" /> if the two values are not equal, otherwise <see langword="false" />.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator !=(FP a, FP b)
		{
			return a.RawValue != b.RawValue;
		}

		/// <summary>
		/// Compares an FP value with an integer value for inequality.
		/// </summary>
		/// <param name="a">The FP value.</param>
		/// <param name="b">The integer value.</param>
		/// <returns><see langword="true" /> if the two values are not equal, otherwise <see langword="false" />.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator !=(FP a, int b)
		{
			return a.RawValue != (long)b << 16;
		}

		/// <summary>
		/// Compares an integer value with an FP value for inequality.
		/// </summary>
		/// <param name="a">The integer value.</param>
		/// <param name="b">The FP value.</param>
		/// <returns><see langword="true" /> if the two values are not equal, otherwise <see langword="false" />.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator !=(int a, FP b)
		{
			return (long)a << 16 != b.RawValue;
		}

		/// <summary>
		/// Converts an integer value to an FP value.
		/// </summary>
		/// <param name="value">The integer value to convert.</param>
		/// <returns>The FP value.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator FP(int value)
		{
			FP result = default(FP);
			result.RawValue = (long)value << 16;
			return result;
		}

		/// <summary>
		/// Converts an integer value to an FP value.
		/// </summary>
		/// <param name="value">The integer value to convert.</param>
		/// <returns>The FP value.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator FP(uint value)
		{
			FP result = default(FP);
			result.RawValue = (long)((ulong)value << 16);
			return result;
		}

		/// <summary>
		/// Converts an integer value to an FP value.
		/// </summary>
		/// <param name="value">The integer value to convert.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator FP(short value)
		{
			FP result = default(FP);
			result.RawValue = (long)value << 16;
			return result;
		}

		/// <summary>
		/// Converts an integer value to an FP value.
		/// </summary>
		/// <param name="value">The integer value to convert.</param>
		/// <returns>The FP value.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator FP(ushort value)
		{
			FP result = default(FP);
			result.RawValue = (long)((ulong)value << 16);
			return result;
		}

		/// <summary>
		/// Implicitly converts a signed byte value to an instance of the FP struct.
		/// </summary>
		/// <param name="value">The signed byte value to be converted.</param>
		/// <returns>An instance of the FP struct representing the converted value.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator FP(sbyte value)
		{
			FP result = default(FP);
			result.RawValue = (long)value << 16;
			return result;
		}

		/// <summary>
		/// Implicit conversion operator for converting a Byte value to an FP (Fixed Point) value.
		/// </summary>
		/// <param name="value">The Byte value to be converted.</param>
		/// <returns>An FP value representing the converted Byte value.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator FP(byte value)
		{
			FP result = default(FP);
			result.RawValue = (long)((ulong)value << 16);
			return result;
		}

		/// <summary>
		/// Converts an integer value to an FP value.
		/// </summary>
		/// <param name="value">The integer value to convert.</param>
		/// <returns>The FP value.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static explicit operator int(FP value)
		{
			return (int)(value.RawValue >> 16);
		}

		/// <summary>
		/// Converts an FP value to an integer value.
		/// </summary>
		/// <param name="value">The FP value to convert.</param>
		/// <returns>The integer value.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static explicit operator long(FP value)
		{
			return value.RawValue >> 16;
		}

		/// <summary>
		/// Converts an FP value to a float value.
		/// </summary>
		/// <param name="value">The FP value to convert.</param>
		/// <returns>The float value.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static explicit operator float(FP value)
		{
			return (float)value.RawValue / 65536f;
		}

		/// <summary>
		/// Converts an FP value to a double value.
		/// </summary>
		/// <param name="value">The FP value to convert.</param>
		/// <returns>The double value.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static explicit operator double(FP value)
		{
			return (double)value.RawValue / 65536.0;
		}

		/// <summary>
		/// Purposefully throws an exception when trying to cast from a float to an FP.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		/// <exception cref="T:System.InvalidOperationException"></exception>
		[Obsolete("Don't cast from float to FP", true)]
		public static implicit operator FP(float value)
		{
			throw new InvalidOperationException();
		}

		/// <summary>
		/// Purposefully throws an exception when trying to cast from a double to an FP.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		/// <exception cref="T:System.InvalidOperationException"></exception>
		[Obsolete("Don't cast from double to FP", true)]
		public static implicit operator FP(double value)
		{
			throw new InvalidOperationException();
		}
	}
}

