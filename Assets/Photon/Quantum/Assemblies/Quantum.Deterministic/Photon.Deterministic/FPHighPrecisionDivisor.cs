using System.Runtime.CompilerServices;

namespace Photon.Deterministic
{
	/// <summary>
	/// Represents a high precision divisor for use with the Fixed Point math system.
	/// </summary>
	public struct FPHighPrecisionDivisor
	{
		/// <summary>
		/// Holds <see cref="T:Photon.Deterministic.FPHighPrecisionDivisor" /> constants in raw (long) form.
		/// </summary>
		public class Raw
		{
			/// <summary>
			/// &gt;Pi number.
			/// <para>Closest double: 3.14159265346825</para>
			/// </summary>
			public const long Pi = 13493037704L;

			/// <summary>
			/// 1/Pi.
			/// <para>Closest double: 0.318309886148199</para>
			/// </summary>
			public const long PiInv = 1367130551L;

			/// <summary>
			/// 2 * Pi.
			/// <para>Closest double: 6.28318530716933</para>
			/// </summary>
			public const long PiTimes2 = 26986075409L;

			/// <summary>
			/// Pi / 2.
			/// <para>Closest double: 1.57079632673413</para>
			/// </summary>
			public const long PiOver2 = 6746518852L;

			/// <summary>
			/// 2 / Pi.
			/// <para>Closest double: 0.636619772296399</para>
			/// </summary>
			public const long PiOver2Inv = 2734261102L;

			/// <summary>
			/// Pi / 4.
			/// <para>Closest double: 0.785398163367063</para>
			/// </summary>
			public const long PiOver4 = 3373259426L;

			/// <summary>
			/// 3 * Pi / 4.
			/// <para>Closest double: 2.35619449010119</para>
			/// </summary>
			public const long Pi3Over4 = 10119778278L;

			/// <summary>
			/// Degrees-to-radians conversion constant.
			/// <para>Closest double: 0.0174532923847437</para>
			/// </summary>
			public const long Deg2Rad = 74961320L;

			/// <summary>
			/// Radians-to-degrees conversion constant.
			/// <para>Closest double: 57.2957795129623</para>
			/// </summary>
			public const long Rad2Deg = 246083499207L;

			/// <summary>
			/// FP constant representing 180 degrees in radian.
			/// <para>Closest double: 3.14159265346825</para>
			/// </summary>
			public const long Rad_180 = 13493037704L;

			/// <summary>
			/// FP constant representing 90 degrees in radian.
			/// <para>Closest double: 1.57079632673413</para>
			/// </summary>
			public const long Rad_90 = 6746518852L;

			/// <summary>
			/// FP constant representing 45 degrees in radian.
			/// <para>Closest double: 0.785398163367063</para>
			/// </summary>
			public const long Rad_45 = 3373259426L;

			/// <summary>
			/// FP constant representing 22.5 degrees in radian.
			/// <para>Closest double: 0.392699081683531</para>
			/// </summary>
			public const long Rad_22_50 = 1686629713L;

			/// <summary>
			/// FP constant representing the number 0.01.
			/// <para>Closest double: 0.00999999977648258</para>
			/// </summary>
			public const long _0_01 = 42949672L;

			/// <summary>
			/// FP constant representing the number 0.02.
			/// <para>Closest double: 0.0199999997857958</para>
			/// </summary>
			public const long _0_02 = 85899345L;

			/// <summary>
			/// FP constant representing the number 0.03.
			/// <para>Closest double: 0.029999999795109</para>
			/// </summary>
			public const long _0_03 = 128849018L;

			/// <summary>
			/// FP constant representing the number 0.04.
			/// <para>Closest double: 0.0399999998044223</para>
			/// </summary>
			public const long _0_04 = 171798691L;

			/// <summary>
			/// FP constant representing the number 0.05.
			/// <para>Closest double: 0.0499999998137355</para>
			/// </summary>
			public const long _0_05 = 214748364L;

			/// <summary>
			/// FP constant representing the number 0.10.
			/// <para>Closest double: 0.0999999998603016</para>
			/// </summary>
			public const long _0_10 = 429496729L;

			/// <summary>
			/// FP constant representing the number 0.20.
			/// <para>Closest double: 0.199999999953434</para>
			/// </summary>
			public const long _0_20 = 858993459L;

			/// <summary>
			/// FP constant representing the number 0.33.
			/// <para>Closest double: 0.333333333255723</para>
			/// </summary>
			public const long _0_33 = 1431655765L;

			/// <summary>
			/// FP constant representing the number 0.99.
			/// <para>Closest double: 0.989999999990687</para>
			/// </summary>
			public const long _0_99 = 4252017623L;

			/// <summary>
			/// FP constant representing the number 1.01.
			/// <para>Closest double: 1.00999999977648</para>
			/// </summary>
			public const long _1_01 = 4337916968L;

			/// <summary>
			/// FP constant representing the number 1.02.
			/// <para>Closest double: 1.0199999997858</para>
			/// </summary>
			public const long _1_02 = 4380866641L;

			/// <summary>
			/// FP constant representing the number 1.03.
			/// <para>Closest double: 1.02999999979511</para>
			/// </summary>
			public const long _1_03 = 4423816314L;

			/// <summary>
			/// FP constant representing the number 1.04.
			/// <para>Closest double: 1.03999999980442</para>
			/// </summary>
			public const long _1_04 = 4466765987L;

			/// <summary>
			/// FP constant representing the number 1.05.
			/// <para>Closest double: 1.04999999981374</para>
			/// </summary>
			public const long _1_05 = 4509715660L;

			/// <summary>
			/// FP constant representing the number 1.10.
			/// <para>Closest double: 1.0999999998603</para>
			/// </summary>
			public const long _1_10 = 4724464025L;

			/// <summary>
			/// FP constant representing the number 1.20.
			/// <para>Closest double: 1.19999999995343</para>
			/// </summary>
			public const long _1_20 = 5153960755L;

			/// <summary>
			/// FP constant representing the number 1.33.
			/// <para>Closest double: 1.33333333325572</para>
			/// </summary>
			public const long _1_33 = 5726623061L;

			/// <summary>
			/// FP constant representing the number 1.99.
			/// <para>Closest double: 1.98999999999069</para>
			/// </summary>
			public const long _1_99 = 8546984919L;

			/// <summary>
			/// FP constant representing the epsilon value EN1.
			/// <para>Closest double: 0.0999999998603016</para>
			/// </summary>
			public const long EN1 = 429496729L;

			/// <summary>
			/// FP constant representing the epsilon value EN2.
			/// <para>Closest double: 0.00999999977648258</para>
			/// </summary>
			public const long EN2 = 42949672L;

			/// <summary>
			/// FP constant representing the epsilon value EN3.
			/// <para>Closest double: 0.000999999931082129</para>
			/// </summary>
			public const long EN3 = 4294967L;

			/// <summary>
			/// FP constant representing the epsilon value EN4.
			/// <para>Closest double: 9.99998301267624E-05</para>
			/// </summary>
			public const long EN4 = 429496L;

			/// <summary>
			/// FP constant representing the epsilon value EN5.
			/// <para>Closest double: 9.99984331429005E-06</para>
			/// </summary>
			public const long EN5 = 42949L;

			/// <summary>
			/// FP constant representing the Euler Number constant.
			/// <para>Closest double: 2.71828182833269</para>
			/// </summary>
			public const long E = 11674931554L;

			/// <summary>
			/// FP constant representing Log(E).
			/// <para>Closest double: 1.44269504072145</para>
			/// </summary>
			public const long Log2_E = 6196328018L;

			/// <summary>
			/// FP constant representing Log(10).
			/// <para>Closest double: 3.32192809483968</para>
			/// </summary>
			public const long Log2_10 = 14267572527L;
		}

		/// <summary>
		/// The extra precision to shift.
		/// </summary>
		public const int ExtraPrecision = 16;

		/// <summary>
		/// The total precision.
		/// </summary>
		public const int TotalPrecision = 32;

		internal long RawValue;

		/// <summary>
		/// &gt;Pi number.
		/// <para>Closest double: 3.14159265346825</para>
		/// </summary>
		public unsafe static FPHighPrecisionDivisor Pi
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 13493037704L;
				return *(FPHighPrecisionDivisor*)(&num);
			}
		}

		/// <summary>
		/// 1/Pi.
		/// <para>Closest double: 0.318309886148199</para>
		/// </summary>
		public unsafe static FPHighPrecisionDivisor PiInv
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 1367130551L;
				return *(FPHighPrecisionDivisor*)(&num);
			}
		}

		/// <summary>
		/// 2 * Pi.
		/// <para>Closest double: 6.28318530716933</para>
		/// </summary>
		public unsafe static FPHighPrecisionDivisor PiTimes2
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 26986075409L;
				return *(FPHighPrecisionDivisor*)(&num);
			}
		}

		/// <summary>
		/// Pi / 2.
		/// <para>Closest double: 1.57079632673413</para>
		/// </summary>
		public unsafe static FPHighPrecisionDivisor PiOver2
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 6746518852L;
				return *(FPHighPrecisionDivisor*)(&num);
			}
		}

		/// <summary>
		/// 2 / Pi.
		/// <para>Closest double: 0.636619772296399</para>
		/// </summary>
		public unsafe static FPHighPrecisionDivisor PiOver2Inv
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 2734261102L;
				return *(FPHighPrecisionDivisor*)(&num);
			}
		}

		/// <summary>
		/// Pi / 4.
		/// <para>Closest double: 0.785398163367063</para>
		/// </summary>
		public unsafe static FPHighPrecisionDivisor PiOver4
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 3373259426L;
				return *(FPHighPrecisionDivisor*)(&num);
			}
		}

		/// <summary>
		/// 3 * Pi / 4.
		/// <para>Closest double: 2.35619449010119</para>
		/// </summary>
		public unsafe static FPHighPrecisionDivisor Pi3Over4
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 10119778278L;
				return *(FPHighPrecisionDivisor*)(&num);
			}
		}

		/// <summary>
		/// Degrees-to-radians conversion constant.
		/// <para>Closest double: 0.0174532923847437</para>
		/// </summary>
		public unsafe static FPHighPrecisionDivisor Deg2Rad
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 74961320L;
				return *(FPHighPrecisionDivisor*)(&num);
			}
		}

		/// <summary>
		/// Radians-to-degrees conversion constant.
		/// <para>Closest double: 57.2957795129623</para>
		/// </summary>
		public unsafe static FPHighPrecisionDivisor Rad2Deg
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 246083499207L;
				return *(FPHighPrecisionDivisor*)(&num);
			}
		}

		/// <summary>
		/// FP constant representing 180 degrees in radian.
		/// <para>Closest double: 3.14159265346825</para>
		/// </summary>
		public unsafe static FPHighPrecisionDivisor Rad_180
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 13493037704L;
				return *(FPHighPrecisionDivisor*)(&num);
			}
		}

		/// <summary>
		/// FP constant representing 90 degrees in radian.
		/// <para>Closest double: 1.57079632673413</para>
		/// </summary>
		public unsafe static FPHighPrecisionDivisor Rad_90
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 6746518852L;
				return *(FPHighPrecisionDivisor*)(&num);
			}
		}

		/// <summary>
		/// FP constant representing 45 degrees in radian.
		/// <para>Closest double: 0.785398163367063</para>
		/// </summary>
		public unsafe static FPHighPrecisionDivisor Rad_45
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 3373259426L;
				return *(FPHighPrecisionDivisor*)(&num);
			}
		}

		/// <summary>
		/// FP constant representing 22.5 degrees in radian.
		/// <para>Closest double: 0.392699081683531</para>
		/// </summary>
		public unsafe static FPHighPrecisionDivisor Rad_22_50
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 1686629713L;
				return *(FPHighPrecisionDivisor*)(&num);
			}
		}

		/// <summary>
		/// FP constant representing the number 0.01.
		/// <para>Closest double: 0.00999999977648258</para>
		/// </summary>
		public unsafe static FPHighPrecisionDivisor _0_01
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 42949672L;
				return *(FPHighPrecisionDivisor*)(&num);
			}
		}

		/// <summary>
		/// FP constant representing the number 0.02.
		/// <para>Closest double: 0.0199999997857958</para>
		/// </summary>
		public unsafe static FPHighPrecisionDivisor _0_02
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 85899345L;
				return *(FPHighPrecisionDivisor*)(&num);
			}
		}

		/// <summary>
		/// FP constant representing the number 0.03.
		/// <para>Closest double: 0.029999999795109</para>
		/// </summary>
		public unsafe static FPHighPrecisionDivisor _0_03
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 128849018L;
				return *(FPHighPrecisionDivisor*)(&num);
			}
		}

		/// <summary>
		/// FP constant representing the number 0.04.
		/// <para>Closest double: 0.0399999998044223</para>
		/// </summary>
		public unsafe static FPHighPrecisionDivisor _0_04
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 171798691L;
				return *(FPHighPrecisionDivisor*)(&num);
			}
		}

		/// <summary>
		/// FP constant representing the number 0.05.
		/// <para>Closest double: 0.0499999998137355</para>
		/// </summary>
		public unsafe static FPHighPrecisionDivisor _0_05
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 214748364L;
				return *(FPHighPrecisionDivisor*)(&num);
			}
		}

		/// <summary>
		/// FP constant representing the number 0.10.
		/// <para>Closest double: 0.0999999998603016</para>
		/// </summary>
		public unsafe static FPHighPrecisionDivisor _0_10
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 429496729L;
				return *(FPHighPrecisionDivisor*)(&num);
			}
		}

		/// <summary>
		/// FP constant representing the number 0.20.
		/// <para>Closest double: 0.199999999953434</para>
		/// </summary>
		public unsafe static FPHighPrecisionDivisor _0_20
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 858993459L;
				return *(FPHighPrecisionDivisor*)(&num);
			}
		}

		/// <summary>
		/// FP constant representing the number 0.33.
		/// <para>Closest double: 0.333333333255723</para>
		/// </summary>
		public unsafe static FPHighPrecisionDivisor _0_33
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 1431655765L;
				return *(FPHighPrecisionDivisor*)(&num);
			}
		}

		/// <summary>
		/// FP constant representing the number 0.99.
		/// <para>Closest double: 0.989999999990687</para>
		/// </summary>
		public unsafe static FPHighPrecisionDivisor _0_99
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 4252017623L;
				return *(FPHighPrecisionDivisor*)(&num);
			}
		}

		/// <summary>
		/// FP constant representing the number 1.01.
		/// <para>Closest double: 1.00999999977648</para>
		/// </summary>
		public unsafe static FPHighPrecisionDivisor _1_01
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 4337916968L;
				return *(FPHighPrecisionDivisor*)(&num);
			}
		}

		/// <summary>
		/// FP constant representing the number 1.02.
		/// <para>Closest double: 1.0199999997858</para>
		/// </summary>
		public unsafe static FPHighPrecisionDivisor _1_02
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 4380866641L;
				return *(FPHighPrecisionDivisor*)(&num);
			}
		}

		/// <summary>
		/// FP constant representing the number 1.03.
		/// <para>Closest double: 1.02999999979511</para>
		/// </summary>
		public unsafe static FPHighPrecisionDivisor _1_03
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 4423816314L;
				return *(FPHighPrecisionDivisor*)(&num);
			}
		}

		/// <summary>
		/// FP constant representing the number 1.04.
		/// <para>Closest double: 1.03999999980442</para>
		/// </summary>
		public unsafe static FPHighPrecisionDivisor _1_04
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 4466765987L;
				return *(FPHighPrecisionDivisor*)(&num);
			}
		}

		/// <summary>
		/// FP constant representing the number 1.05.
		/// <para>Closest double: 1.04999999981374</para>
		/// </summary>
		public unsafe static FPHighPrecisionDivisor _1_05
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 4509715660L;
				return *(FPHighPrecisionDivisor*)(&num);
			}
		}

		/// <summary>
		/// FP constant representing the number 1.10.
		/// <para>Closest double: 1.0999999998603</para>
		/// </summary>
		public unsafe static FPHighPrecisionDivisor _1_10
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 4724464025L;
				return *(FPHighPrecisionDivisor*)(&num);
			}
		}

		/// <summary>
		/// FP constant representing the number 1.20.
		/// <para>Closest double: 1.19999999995343</para>
		/// </summary>
		public unsafe static FPHighPrecisionDivisor _1_20
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 5153960755L;
				return *(FPHighPrecisionDivisor*)(&num);
			}
		}

		/// <summary>
		/// FP constant representing the number 1.33.
		/// <para>Closest double: 1.33333333325572</para>
		/// </summary>
		public unsafe static FPHighPrecisionDivisor _1_33
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 5726623061L;
				return *(FPHighPrecisionDivisor*)(&num);
			}
		}

		/// <summary>
		/// FP constant representing the number 1.99.
		/// <para>Closest double: 1.98999999999069</para>
		/// </summary>
		public unsafe static FPHighPrecisionDivisor _1_99
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 8546984919L;
				return *(FPHighPrecisionDivisor*)(&num);
			}
		}

		/// <summary>
		/// FP constant representing the epsilon value EN1.
		/// <para>Closest double: 0.0999999998603016</para>
		/// </summary>
		public unsafe static FPHighPrecisionDivisor EN1
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 429496729L;
				return *(FPHighPrecisionDivisor*)(&num);
			}
		}

		/// <summary>
		/// FP constant representing the epsilon value EN2.
		/// <para>Closest double: 0.00999999977648258</para>
		/// </summary>
		public unsafe static FPHighPrecisionDivisor EN2
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 42949672L;
				return *(FPHighPrecisionDivisor*)(&num);
			}
		}

		/// <summary>
		/// FP constant representing the epsilon value EN3.
		/// <para>Closest double: 0.000999999931082129</para>
		/// </summary>
		public unsafe static FPHighPrecisionDivisor EN3
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 4294967L;
				return *(FPHighPrecisionDivisor*)(&num);
			}
		}

		/// <summary>
		/// FP constant representing the epsilon value EN4.
		/// <para>Closest double: 9.99998301267624E-05</para>
		/// </summary>
		public unsafe static FPHighPrecisionDivisor EN4
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 429496L;
				return *(FPHighPrecisionDivisor*)(&num);
			}
		}

		/// <summary>
		/// FP constant representing the epsilon value EN5.
		/// <para>Closest double: 9.99984331429005E-06</para>
		/// </summary>
		public unsafe static FPHighPrecisionDivisor EN5
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 42949L;
				return *(FPHighPrecisionDivisor*)(&num);
			}
		}

		/// <summary>
		/// FP constant representing the Euler Number constant.
		/// <para>Closest double: 2.71828182833269</para>
		/// </summary>
		public unsafe static FPHighPrecisionDivisor E
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 11674931554L;
				return *(FPHighPrecisionDivisor*)(&num);
			}
		}

		/// <summary>
		/// FP constant representing Log(E).
		/// <para>Closest double: 1.44269504072145</para>
		/// </summary>
		public unsafe static FPHighPrecisionDivisor Log2_E
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 6196328018L;
				return *(FPHighPrecisionDivisor*)(&num);
			}
		}

		/// <summary>
		/// FP constant representing Log(10).
		/// <para>Closest double: 3.32192809483968</para>
		/// </summary>
		public unsafe static FPHighPrecisionDivisor Log2_10
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				long num = 14267572527L;
				return *(FPHighPrecisionDivisor*)(&num);
			}
		}

		/// <summary>
		/// Returns the value of the divisor as a <see cref="T:Photon.Deterministic.FP" />.
		/// </summary>
		public readonly FP AsFP => FP.FromRaw(RawValue >> 16);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static long RawMod(long standardPrecisionRaw, long highPrecisionRaw)
		{
			long num = standardPrecisionRaw;
			int num2 = (int)((ulong)num >> 63);
			num <<= 16;
			num %= highPrecisionRaw;
			num += (num2 << 16) - num2;
			return num >> 16;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static long RawModPositive(long standardPrecisionRaw, long highPrecisionRaw)
		{
			long num = standardPrecisionRaw;
			num <<= 16;
			num %= highPrecisionRaw;
			return num >> 16;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static long RawDiv(long standardPrecisionRaw, long highPrecisionRaw)
		{
			long num = standardPrecisionRaw;
			long num2 = (long)((ulong)num >> 63);
			num <<= 32;
			num += (num2 << 32) - num2;
			return num / highPrecisionRaw;
		}
	}
}

