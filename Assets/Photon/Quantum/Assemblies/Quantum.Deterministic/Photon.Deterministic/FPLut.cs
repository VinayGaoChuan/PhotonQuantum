using System;
using System.IO;

namespace Photon.Deterministic
{
	/// <summary>
	/// The lookup table for the inverse cosine function.
	/// The table stores precalculated values of the inverse cosine function for specific angles.
	/// The values are in fixed-point format with a precision of 16 bits.
	/// </summary>
	public static class FPLut
	{
		/// <summary>
		/// The number of bits used for the fractional part of the fixed-point numbers.
		/// </summary>
		public const int PRECISION = 16;

		/// <summary>
		/// The value of PI in fixed-point format.
		/// </summary>
		public const long PI = 205887L;

		/// <summary>
		/// The value of PI times 2 in fixed-point format.
		/// </summary>
		public const long PITIMES2 = 411775L;

		/// <summary>
		/// The value of PI divided by 2 in fixed-point format.
		/// </summary>
		public const long PIOVER2 = 102944L;

		/// <summary>
		/// The value of one in fixed-point format.
		/// </summary>
		public const long ONE = 65536L;

		internal const int SQRT_RESOLUTION_SPACE = 3;

		internal const int SQRT_LUT_SIZE_BASE_2 = 16;

		internal const int SQRT_VALUE_STEP = 3;

		internal const int SQRT_LUT_SIZE_BASE_10 = 65536;

		internal const int SqrtAdditionalPrecisionBits = 6;

		internal const int Log2LutSizeExponent = 6;

		internal const int Log2AdditionalPrecisionBits = 15;

		/// <summary>
		/// How much Log2 additional precision (AP) result needs to be shifted to allow for a safe FPHighPrecision division.
		/// There must be a way to calculate this, but I have a brain fog atm.
		/// 6 is the safe choice here - max log2 is 48.
		/// </summary>
		internal const int Log2APShiftForHPDivision = 6;

		internal const int ExpNegativeLutPrecision = 42;

		internal const int ExpNegativeLutCount = 30;

		internal const int ExpNonNegativeLutCount = 33;

		internal const int ExpOverflowingThreshold = 20;

		/// <summary>
		/// Lookup table for approximate square root values.
		/// </summary>
		public static int[] sqrt_aprox_lut;

		/// <summary>
		/// Lookup table for the arcsine function.
		/// </summary>
		public static long[] asin_lut;

		/// <summary>
		/// A lookup table used for approximately calculating the inverse cosine (acos) function for fixed-point numbers.
		/// </summary>
		public static long[] acos_lut;

		/// <summary>
		/// Lookup table for the Atan function in FPMath
		/// </summary>
		public static long[] atan_lut;

		/// <summary>
		/// Lookup table for the sine and cosine functions.
		/// </summary>
		public static long[] sin_cos_lut;

		/// <summary>
		/// Lookup table for the tangent function.
		/// </summary>
		public static long[] tan_lut;

		/// <summary>
		/// Lookup table for the log2 function.
		/// </summary>
		public static uint[] log2_approx_lut;

		/// <summary>
		/// Lookup table for the exp function.
		/// </summary>
		public static long[] exp_integral_lut;

		/// <summary>
		/// Returns <see langword="true" /> if the lookup tables have been loaded.
		/// </summary>
		public static bool IsLoaded
		{
			get
			{
				if (sin_cos_lut != null && sin_cos_lut.Length != 0 && tan_lut != null && tan_lut.Length != 0 && asin_lut != null && asin_lut.Length != 0 && acos_lut != null && acos_lut.Length != 0 && atan_lut != null && atan_lut.Length != 0 && sqrt_aprox_lut != null)
				{
					return sqrt_aprox_lut.Length != 0;
				}
				return false;
			}
		}

		/// <summary>
		/// Initialize LUT from directory <paramref name="directoryPath" />. The directory needs to have following files:
		/// * FPSin.bytes
		/// * FPCos.bytes
		/// * FPTan.bytes
		/// * FPAsin.bytes
		/// * FPAcos.bytes
		/// * FPAtan.bytes
		/// * FPSqrt.bytes
		/// </summary>
		/// <param name="directoryPath"></param>
		public static void Init(string directoryPath)
		{
			Load(directoryPath, "FPSinCos", ref sin_cos_lut);
			Load(directoryPath, "FPTan", ref tan_lut);
			Load(directoryPath, "FPAsin", ref asin_lut);
			Load(directoryPath, "FPAcos", ref acos_lut);
			Load(directoryPath, "FPAtan", ref atan_lut);
			Load(directoryPath, "FPSqrt", ref sqrt_aprox_lut);
			InitSmallLut();
		}

		/// <summary>
		/// Initialize LUT using <paramref name="lutProvider" />. The provider needs to be able to load following paths:
		/// * FPSin
		/// * FPCos
		/// * FPTan
		/// * FPAsin
		/// * FPAcos
		/// * FPAtan
		/// * FPSqrt
		/// </summary>
		/// <param name="lutProvider"></param>
		public static void Init(LutProvider lutProvider)
		{
			Load(lutProvider, "FPSinCos", ref sin_cos_lut);
			Load(lutProvider, "FPTan", ref tan_lut);
			Load(lutProvider, "FPAsin", ref asin_lut);
			Load(lutProvider, "FPAcos", ref acos_lut);
			Load(lutProvider, "FPAtan", ref atan_lut);
			Load(lutProvider, "FPSqrt", ref sqrt_aprox_lut);
			InitSmallLut();
		}

		/// <summary>
		/// Initialize LUT using using byte arrays.
		/// </summary>
		public static void Init(byte[] sinCos, byte[] tan, byte[] asin, byte[] acos, byte[] atan, byte[] sqrt)
		{
			Load(sinCos ?? throw new ArgumentNullException("sinCos"), ref sin_cos_lut);
			Load(tan ?? throw new ArgumentNullException("tan"), ref tan_lut);
			Load(asin ?? throw new ArgumentNullException("asin"), ref asin_lut);
			Load(acos ?? throw new ArgumentNullException("acos"), ref acos_lut);
			Load(atan ?? throw new ArgumentNullException("atan"), ref atan_lut);
			Load(sqrt ?? throw new ArgumentNullException("sqrt"), ref sqrt_aprox_lut);
			InitSmallLut();
		}

		private static void InitSmallLut()
		{
			log2_approx_lut = new uint[66]
			{
				0u, 48034513u, 95335645u, 141925456u, 187825021u, 233054496u, 277633165u, 321579490u, 364911162u, 407645136u,
				449797678u, 491384396u, 532420281u, 572919734u, 612896598u, 652364189u, 691335320u, 729822324u, 767837083u, 805391046u,
				842495250u, 879160341u, 915396590u, 951213914u, 986621888u, 1021629764u, 1056246482u, 1090480686u, 1124340739u, 1157834731u,
				1190970490u, 1223755601u, 1256197405u, 1288303019u, 1320079339u, 1351533050u, 1382670639u, 1413498396u, 1444022426u, 1474248656u,
				1504182841u, 1533830570u, 1563197273u, 1592288229u, 1621108567u, 1649663276u, 1677957208u, 1705995083u, 1733781493u, 1761320910u,
				1788617686u, 1815676059u, 1842500157u, 1869094003u, 1895461516u, 1921606515u, 1947532725u, 1973243777u, 1998743213u, 2024034488u,
				2049120974u, 2074005959u, 2098692655u, 2123184198u, 2147483648u, 2171593995u
			};
			exp_integral_lut = new long[63]
			{
				0L, 1L, 3L, 8L, 22L, 61L, 166L, 451L, 1226L, 3334L,
				9065L, 24641L, 66982L, 182076L, 494934L, 1345372L, 3657101L, 9941033L, 27022531L, 73454856L,
				199671002L, 542762058L, 1475380240L, 4010499297L, 10901667362L, 29633804291L, 80553031713L, 218965842333L, 595210870268L, 1617950892750L,
				65536L, 178145L, 484249L, 1316325L, 3578144L, 9726404L, 26439109L, 71868950L, 195360062L, 531043708L,
				1443526462L, 3923911751L, 10666298010L, 28994004058L, 78813874367L, 214238322522L, 582360139072L, 1583018983658L, 4303091737384L, 11697016075923L,
				31795786246376L, 1318815734L, 3584912846L, 9744803446L, 26489122130L, 72004899337L, 195729609429L, 532048240602L, 1446257064291L, 3931334297144L,
				10686474581524L, 29048849665247L, 78962960182681L
			};
		}

		/// <summary>
		/// Generate lookup tables in <paramref name="directoryPath" />.
		/// </summary>
		/// <param name="directoryPath"></param>
		public static void GenerateTables(string directoryPath)
		{
			LutGenerator.Generate(directoryPath);
		}

		private unsafe static void Load<T>(LutProvider lutProvider, string path, ref T[] lut) where T : unmanaged
		{
			byte[] array = lutProvider(path);
			lut = new T[array.Length / sizeof(T)];
			Buffer.BlockCopy(array, 0, lut, 0, array.Length);
		}

		private unsafe static void Load<T>(string directoryPath, string filePath, ref T[] lut) where T : unmanaged
		{
			byte[] array = File.ReadAllBytes(Path.Combine(directoryPath, filePath) + ".bytes");
			lut = new T[array.Length / sizeof(T)];
			Buffer.BlockCopy(array, 0, lut, 0, array.Length);
		}

		private unsafe static void Load<T>(byte[] data, ref T[] lut) where T : unmanaged
		{
			lut = new T[data.Length / sizeof(T)];
			Buffer.BlockCopy(data, 0, lut, 0, data.Length);
		}
	}
}

