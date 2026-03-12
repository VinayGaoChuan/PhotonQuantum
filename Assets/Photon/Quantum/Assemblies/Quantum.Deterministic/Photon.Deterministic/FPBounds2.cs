using System;
using System.Runtime.InteropServices;

namespace Photon.Deterministic
{
	/// <summary>
	/// Represents an 2D axis aligned bounding box (AABB).
	/// </summary>
	/// \ingroup MathAPI
	[Serializable]
	[StructLayout(LayoutKind.Explicit)]
	public struct FPBounds2
	{
		/// <summary>
		/// The size of the struct in-memory inside the Frame data-buffers or stack (when passed as value parameter).
		/// Not related to the snapshot payload this occupies, which is bit-packed and compressed.
		/// </summary>
		public const int SIZE = 32;

		/// <summary>
		/// Center of the bounding box.
		/// </summary>
		[FieldOffset(0)]
		public FPVector2 Center;

		/// <summary>
		/// Extents of the bounding box (half of the size).
		/// </summary>
		[FieldOffset(16)]
		public FPVector2 Extents;

		/// <summary>
		/// Gets or sets the maximal point of the box. This is always equal to <see cref="F:Photon.Deterministic.FPBounds2.Center" /> + <see cref="F:Photon.Deterministic.FPBounds2.Extents" />.
		/// Setting this property will not affect <see cref="P:Photon.Deterministic.FPBounds2.Min" />.
		/// </summary>
		public FPVector2 Max
		{
			readonly get
			{
				return Center + Extents;
			}
			set
			{
				SetMinMax(Min, value);
			}
		}

		/// <summary>
		/// Gets or sets the minimal point of the box. This is always equal to <see cref="F:Photon.Deterministic.FPBounds2.Center" /> - <see cref="F:Photon.Deterministic.FPBounds2.Extents" />.
		/// Setting this property will not affect <see cref="P:Photon.Deterministic.FPBounds2.Max" />.
		/// </summary>
		public FPVector2 Min
		{
			readonly get
			{
				return Center - Extents;
			}
			set
			{
				SetMinMax(value, Max);
			}
		}

		/// <summary>
		/// Create a new Bounds with the given center and extents.
		/// </summary>
		/// <param name="center">Center point.</param>
		/// <param name="extents">Extents (half the size).</param>
		public FPBounds2(FPVector2 center, FPVector2 extents)
		{
			Center = center;
			Extents = extents;
		}

		/// <summary>
		/// Expand bounds by 0.5 * <paramref name="amount" /> in both directions.
		/// </summary>
		/// <param name="amount"></param>
		public void Expand(FP amount)
		{
			Extents += new FPVector2(amount * FP._0_50, amount * FP._0_50);
		}

		/// <summary>
		/// Expand bounds by 0.5 * <paramref name="amount" /> in both directions.
		/// </summary>
		/// <param name="amount"></param>
		public void Expand(FPVector2 amount)
		{
			Extents += amount * FP._0_50;
		}

		/// <summary>
		/// Set the bounds to the given min and max points.
		/// </summary>
		/// <param name="min">Minimum position.</param>
		/// <param name="max">Maximum position.</param>
		public void SetMinMax(FPVector2 min, FPVector2 max)
		{
			Extents = (max - min) * FP._0_50;
			Center = min + Extents;
		}

		/// <summary>
		/// Expand bounds to contain <paramref name="point" /> (if needed).
		/// </summary>
		/// <param name="point"></param>
		public void Encapsulate(FPVector2 point)
		{
			SetMinMax(FPVector2.Min(Min, point), FPVector2.Max(Max, point));
		}

		/// <summary>
		/// Expand bounds to contain <paramref name="bounds" /> (if needed).
		/// </summary>
		/// <param name="bounds"></param>
		public void Encapsulate(FPBounds2 bounds)
		{
			Encapsulate(bounds.Center - bounds.Extents);
			Encapsulate(bounds.Center + bounds.Extents);
		}

		/// <summary>
		/// Returns <see langword="true" /> if there is an intersection between bounds.
		/// </summary>
		/// <param name="bounds"></param>
		/// <returns></returns>
		public readonly bool Intersects(FPBounds2 bounds)
		{
			if (Min.X <= bounds.Max.X && Max.X >= bounds.Min.X && Min.Y <= bounds.Max.Y)
			{
				return Max.Y >= bounds.Min.Y;
			}
			return false;
		}

		/// <summary>
		/// Returns <see langword="true" /> if the <paramref name="point" /> is inside the bounds.
		/// </summary>
		/// <param name="point"></param>
		/// <returns></returns>
		public readonly bool Contains(FPVector2 point)
		{
			FP fP = FPMath.Abs(point.X - Center.X);
			FP fP2 = FPMath.Abs(point.Y - Center.Y);
			if (fP <= Extents.X)
			{
				return fP2 <= Extents.Y;
			}
			return false;
		}

		/// <summary>
		/// Serializes the FPBounds2 struct by serializing its FPVector2 fields.
		/// </summary>
		/// <param name="ptr">A pointer to the FPBounds2 struct to be serialized.</param>
		/// <param name="serializer">The serializer object used to serialize the components.</param>
		public unsafe static void Serialize(void* ptr, IDeterministicFrameSerializer serializer)
		{
			FPVector2.Serialize(&((FPBounds2*)ptr)->Center, serializer);
			FPVector2.Serialize(&((FPBounds2*)ptr)->Extents, serializer);
		}

		/// <summary>
		/// Computes the hash code of the FPBounds2 instance.
		/// </summary>
		/// <returns>The hash code of the FPBounds2 object.</returns>
		public override readonly int GetHashCode()
		{
			int num = 17;
			num = num * 31 + Center.GetHashCode();
			return num * 31 + Extents.GetHashCode();
		}
	}
}

