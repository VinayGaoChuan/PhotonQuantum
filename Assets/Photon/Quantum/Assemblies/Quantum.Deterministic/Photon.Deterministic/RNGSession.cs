using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Photon.Deterministic
{
	/// <summary>
	/// PCG32 random generator, 16 bytes in size.
	/// <a href="http://www.pcg-random.org">http://www.pcg-random.org</a>
	/// </summary>
	[StructLayout(LayoutKind.Explicit)]
	public struct RNGSession
	{
		/// <summary>
		/// The size of the struct in-memory inside the Frame data-buffers or stack (when passed as value parameter).
		/// Not related to the snapshot payload this occupies, which is bit-packed and compressed.
		/// </summary>
		public const int SIZE = 16;

		/// <summary>
		/// The maximum possible value to generate.
		/// </summary>
		public const uint MAX = uint.MaxValue;

		[FieldOffset(0)]
		private ulong state;

		[FieldOffset(8)]
		private ulong inc;

		/// <summary>
		/// Returns a copy of this RNGSession, can be used to check what next random values will be
		/// without affecting the state.
		/// </summary>
		public readonly RNGSession Peek
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				return this;
			}
		}

		/// <summary>
		/// Returns a random FP within [0.0, 1.0).
		/// </summary>
		/// <returns>A random FP within [0.0, 1.0)</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public FP Next()
		{
			return NextFP();
		}

		/// <summary>
		/// Returns a random FP within [0.0, 1.0].
		/// </summary>
		/// <returns>A random FP within [0.0, 1.0]</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public FP NextInclusive()
		{
			return FP.FromRaw(NextUnbiased(65537u));
		}

		/// <summary>
		/// Returns a random FP within [<paramref name="minInclusive" />, <paramref name="maxExclusive" />).
		/// </summary>
		/// <returns>A random FP within [<paramref name="minInclusive" />, <paramref name="maxExclusive" />)</returns>
		public FP Next(FP minInclusive, FP maxExclusive)
		{
			if (minInclusive > maxExclusive)
			{
				FP fP = minInclusive;
				minInclusive = maxExclusive;
				maxExclusive = fP;
			}
			return minInclusive + FP.MulTruncate(Next(), maxExclusive - minInclusive);
		}

		/// <summary>
		/// Returns a random FP within [<paramref name="minInclusive" />, <paramref name="maxInclusive" />].
		/// </summary>
		/// <returns>A random FP within [<paramref name="minInclusive" />, <paramref name="maxInclusive" />]</returns>
		public FP NextInclusive(FP minInclusive, FP maxInclusive)
		{
			if (minInclusive > maxInclusive)
			{
				FP fP = minInclusive;
				minInclusive = maxInclusive;
				maxInclusive = fP;
			}
			return minInclusive + FP.MulTruncate(NextInclusive(), maxInclusive - minInclusive);
		}

		/// <summary>
		/// Returns a random int within [<paramref name="minInclusive" />, <paramref name="maxExclusive" />].
		/// </summary>
		/// <returns>A random int within [<paramref name="minInclusive" />, <paramref name="maxExclusive" />]</returns>
		public int Next(int minInclusive, int maxExclusive)
		{
			if (minInclusive == maxExclusive)
			{
				return minInclusive;
			}
			if (minInclusive > maxExclusive)
			{
				int num = minInclusive;
				minInclusive = maxExclusive;
				maxExclusive = num;
			}
			uint max = (uint)(maxExclusive - minInclusive);
			uint num2 = NextUnbiased(max);
			return (int)(minInclusive + num2);
		}

		/// <summary>
		/// Returns a random int within [<paramref name="minInclusive" />, <paramref name="maxInclusive" />].
		/// </summary>
		/// <returns>A random int within [<paramref name="minInclusive" />, <paramref name="maxInclusive" />]</returns>
		public int NextInclusive(int minInclusive, int maxInclusive)
		{
			if (minInclusive == maxInclusive)
			{
				return minInclusive;
			}
			if (minInclusive > maxInclusive)
			{
				int num = minInclusive;
				minInclusive = maxInclusive;
				maxInclusive = num;
			}
			uint num2 = (uint)(maxInclusive - minInclusive + 1);
			if (num2 == 0)
			{
				return (int)NextUInt32();
			}
			uint num3 = NextUnbiased(num2);
			return (int)(minInclusive + num3);
		}

		private uint NextUnbiased(uint max)
		{
			uint num = (0 - max) % max;
			uint num2;
			do
			{
				num2 = NextUInt32();
			}
			while (num2 < num);
			return num2 % max;
		}

		/// <summary>
		/// Create a new RNGSession with the given seed.
		/// </summary>
		/// <param name="seed">The random number generator seed.</param>
		public RNGSession(int seed)
		{
			ulong x = (ulong)seed;
			state = NextSplitMix64(ref x);
			inc = NextSplitMix64(ref x);
		}

		internal uint NextUInt32()
		{
			ulong num = state;
			state = num * 6364136223846793005L + (inc | 1);
			uint num2 = (uint)(((num >> 18) ^ num) >> 27);
			int num3 = (int)(num >> 59);
			return (num2 >> num3) | (num2 << (-num3 & 0x1F));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal FP NextFP()
		{
			return FP.FromRaw(NextUInt32() >> 16);
		}

		/// <summary>
		/// Returns a string representation of the RNGSession.
		/// </summary>
		public override readonly string ToString()
		{
			return string.Format("{0}:{1}, {2}:{3}", "state", state, "inc", inc);
		}

		/// <summary>
		/// Serializes a <see cref="T:Photon.Deterministic.RNGSession" /> into a <see cref="T:Photon.Deterministic.IDeterministicFrameSerializer" /> to write or read from a frame snapshot.
		/// </summary>
		/// <param name="ptr">The pointer to the <see cref="T:Photon.Deterministic.RNGSession" />.</param>
		/// <param name="serializer">The <see cref="T:Photon.Deterministic.IDeterministicFrameSerializer" /> instance into which the struct will be serialized.</param>
		public unsafe static void Serialize(void* ptr, IDeterministicFrameSerializer serializer)
		{
			serializer.Stream.Serialize(&((RNGSession*)ptr)->state);
			serializer.Stream.Serialize(&((RNGSession*)ptr)->inc);
		}

		/// <summary>
		/// Overrides the default hash function.
		/// </summary>
		/// <returns>A hash code for the current object.</returns>
		public override readonly int GetHashCode()
		{
			int num = 17;
			num = num * 31 + state.GetHashCode();
			return num * 31 + inc.GetHashCode();
		}

		private static ulong NextSplitMix64(ref ulong x)
		{
			ulong num = (x += 11400714819323198485uL);
			num = (num ^ (num >> 30)) * 13787848793156543929uL;
			num = (num ^ (num >> 27)) * 10723151780598845931uL;
			return num ^ (num >> 31);
		}
	}
}

