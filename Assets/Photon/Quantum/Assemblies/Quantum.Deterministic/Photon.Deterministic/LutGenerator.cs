using System;
using System.IO;

namespace Photon.Deterministic
{
	internal class LutGenerator
	{
		private const long ATAN_DENSITY1_COVER = 6L;

		public const long ATAN_SIZE_DENSITY1 = 393216L;

		public const int ATAN_DENSITY2_COVER = 250;

		public const long ATAN_DENSITY2_COVER_RAW = 16384000L;

		public const long ATAN_SIZE_DENSITY2 = 3904L;

		public const int ATAN_DENSITY3_COVER = 10000;

		public const int ATAN_DENSITY3_COVER_RAW = 655360000;

		public const long ATAN_SIZE_DENSITY3 = 609L;

		public const string TABLE_NAME_SIN_COS = "FPSinCos";

		public const string TABLE_NAME_TAN = "FPTan";

		public const string TABLE_NAME_ASIN = "FPAsin";

		public const string TABLE_NAME_ACOS = "FPAcos";

		public const string TABLE_NAME_ATAN = "FPAtan";

		public const string TABLE_NAME_SQRT = "FPSqrt";

		private static void Generate(Func<double, double> op, string file, long min, long max)
		{
			file += ".bytes";
			if (File.Exists(file))
			{
				File.Delete(file);
			}
			using FileStream fileStream = File.OpenWrite(file);
			for (long num = min; num <= max; num++)
			{
				long value = (long)Math.Round(op((double)num / 65536.0) * 65536.0);
				fileStream.Write(BitConverter.GetBytes(value), 0, 8);
			}
			fileStream.Flush();
		}

		private static void GenerateSqrt(string file)
		{
			file += ".bytes";
			if (File.Exists(file))
			{
				File.Delete(file);
			}
			using FileStream fileStream = File.OpenWrite(file);
			for (long num = 0L; num < 65536; num++)
			{
				double num2 = Math.Sqrt(FP.FromRaw(num).AsDouble);
				num2 *= 64.0;
				int value = checked((int)Math.Round(num2 * 65536.0));
				byte[] bytes = BitConverter.GetBytes(value);
				fileStream.Write(bytes, 0, bytes.Length);
			}
			byte[] bytes2 = BitConverter.GetBytes((int)(FP._1.RawValue << 6));
			fileStream.Write(bytes2, 0, bytes2.Length);
			fileStream.Flush();
		}

		public static void GenerateSinCosPacked(string file)
		{
			file += ".bytes";
			if (File.Exists(file))
			{
				File.Delete(file);
			}
			using FileStream fileStream = File.OpenWrite(file);
			for (long num = 0L; num <= 205887; num++)
			{
				FP fP = FP.FromRaw(num);
				long rawValue = FP.FromFloat_UNSAFE((float)Math.Sin(fP.AsDouble)).RawValue;
				long rawValue2 = FP.FromFloat_UNSAFE((float)Math.Cos(fP.AsDouble)).RawValue;
				int num2;
				int num3;
				checked
				{
					num2 = (int)rawValue;
					num3 = (int)rawValue2;
				}
				long num4 = (uint)num2;
				num4 |= ((long)num3 << 32) & -4294967296L;
				fileStream.Write(BitConverter.GetBytes(num4), 0, 8);
			}
			for (long num5 = 1L; num5 <= 205887; num5++)
			{
				FP fP2 = FP.FromRaw(num5);
				FP fP3 = FP.FromRaw(205887 - num5);
				long num6 = -FP.FromFloat_UNSAFE((float)Math.Sin(fP2.AsDouble)).RawValue;
				long rawValue3 = FP.FromFloat_UNSAFE((float)Math.Cos(fP3.AsDouble)).RawValue;
				int num7;
				int num8;
				checked
				{
					num7 = (int)num6;
					num8 = (int)rawValue3;
				}
				long num9 = (uint)num7;
				num9 |= ((long)num8 << 32) & -4294967296L;
				fileStream.Write(BitConverter.GetBytes(num9), 0, 8);
			}
		}

		public static void GenerateAtan(string file)
		{
			file += ".bytes";
			if (File.Exists(file))
			{
				File.Delete(file);
			}
			using FileStream fileStream = File.OpenWrite(file);
			for (long num = 0L; num <= 393216; num++)
			{
				long value = (long)Math.Round(Math.Atan((double)num / 65536.0) * 65536.0);
				fileStream.Write(BitConverter.GetBytes(value), 0, 8);
			}
			FP fP = FP.FromRaw(393216L);
			long num2 = 4096L;
			for (long num3 = 0L; num3 < 3904; num3++)
			{
				fP.RawValue += num2;
				long value2 = (long)Math.Round(Math.Atan(fP.AsDouble) * 65536.0);
				fileStream.Write(BitConverter.GetBytes(value2), 0, 8);
			}
			long num4 = 1048576L;
			for (long num5 = 0L; num5 < 609; num5++)
			{
				fP.RawValue += num4;
				long value3 = (long)Math.Round(Math.Atan(fP.AsDouble) * 65536.0);
				fileStream.Write(BitConverter.GetBytes(value3), 0, 8);
			}
			fileStream.Flush();
		}

		public static void Generate(string directoryPath)
		{
			GenerateSinCosPacked(Path.Combine(directoryPath, "FPSinCos"));
			Generate(Math.Tan, Path.Combine(directoryPath, "FPTan"), -205887L, 205887L);
			Generate(Math.Asin, Path.Combine(directoryPath, "FPAsin"), -65536L, 65536L);
			Generate(Math.Acos, Path.Combine(directoryPath, "FPAcos"), -65536L, 65536L);
			GenerateAtan(Path.Combine(directoryPath, "FPAtan"));
			GenerateSqrt(Path.Combine(directoryPath, "FPSqrt"));
		}
	}
}

