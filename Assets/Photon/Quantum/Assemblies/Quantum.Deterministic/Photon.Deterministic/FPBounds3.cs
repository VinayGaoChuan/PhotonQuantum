using System;
using System.Runtime.InteropServices;

namespace Photon.Deterministic
{
	/// <summary>
	/// Represents an 3D axis aligned bounding box (AABB).
	/// </summary>
	/// \ingroup MathAPI
	[Serializable]
	[StructLayout(LayoutKind.Explicit)]
	public struct FPBounds3
	{
		/// <summary>
		/// /// <summary>
		/// The size of the struct in-memory inside the Frame data-buffers or stack (when passed as value parameter).
		/// Not related to the snapshot payload this occupies, which is bit-packed and compressed.
		/// </summary>
		/// </summary>
		public const int SIZE = 48;

		/// <summary>
		/// Center of the bounding box.
		/// </summary>
		[FieldOffset(0)]
		public FPVector3 Center;

		/// <summary>
		/// Extents of the bounding box (half of the size).
		/// </summary>
		[FieldOffset(24)]
		public FPVector3 Extents;

		/// <summary>
		/// Gets or sets the maximal point of the box. This is always equal to <see cref="F:Photon.Deterministic.FPBounds3.Center" /> + <see cref="F:Photon.Deterministic.FPBounds3.Extents" />.
		/// Setting this property will not affect <see cref="P:Photon.Deterministic.FPBounds3.Min" />.
		/// </summary>
		public FPVector3 Max
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
		/// Gets or sets the minimal point of the box. This is always equal to <see cref="F:Photon.Deterministic.FPBounds3.Center" /> - <see cref="F:Photon.Deterministic.FPBounds3.Extents" />.
		/// Setting this property will not affect <see cref="P:Photon.Deterministic.FPBounds3.Max" />.
		/// </summary>
		public FPVector3 Min
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
		public FPBounds3(FPVector3 center, FPVector3 extents)
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
			Extents += new FPVector3(amount * FP._0_50, amount * FP._0_50, amount * FP._0_50);
		}

		/// <summary>
		/// Expand bounds by 0.5 * <paramref name="amount" /> in both directions.
		/// </summary>
		/// <param name="amount"></param>
		public void Expand(FPVector3 amount)
		{
			Extents += amount * FP._0_50;
		}

		/// <summary>
		/// Set the bounds to the given <paramref name="min" /> and <paramref name="max" /> points.
		/// </summary>
		/// <param name="min">Minimum position.</param>
		/// <param name="max">Maximum position.</param>
		public void SetMinMax(FPVector3 min, FPVector3 max)
		{
			Extents = (max - min) * FP._0_50;
			Center = min + Extents;
		}

		/// <summary>
		/// Expand bounds to contain <paramref name="point" /> (if needed).
		/// </summary>
		/// <param name="point"></param>
		public void Encapsulate(FPVector3 point)
		{
			SetMinMax(FPVector3.Min(Min, point), FPVector3.Max(Max, point));
		}

		/// <summary>
		/// Expand bounds to contain <paramref name="bounds" /> (if needed).
		/// </summary>
		/// <param name="bounds"></param>
		public void Encapsulate(FPBounds3 bounds)
		{
			Encapsulate(bounds.Center - bounds.Extents);
			Encapsulate(bounds.Center + bounds.Extents);
		}

		/// <summary>
		/// Returns <see langword="true" /> if there is an intersection between bounds.
		/// </summary>
		/// <param name="bounds"></param>
		/// <returns></returns>
		public readonly bool Intersects(FPBounds3 bounds)
		{
			if (Min.X <= bounds.Max.X && Max.X >= bounds.Min.X && Min.Y <= bounds.Max.Y && Max.Y >= bounds.Min.Y && Min.Z <= bounds.Max.Z)
			{
				return Max.Z >= bounds.Min.Z;
			}
			return false;
		}

		/// <summary>
		/// Returns <see langword="true" /> if the <paramref name="point" /> is inside the bounds.
		/// </summary>
		/// <param name="point"></param>
		/// <returns></returns>
		public readonly bool Contains(FPVector3 point)
		{
			FP fP = FPMath.Abs(point.X - Center.X);
			FP fP2 = FPMath.Abs(point.Y - Center.Y);
			FP fP3 = FPMath.Abs(point.Z - Center.Z);
			if (fP <= Extents.X && fP2 <= Extents.Y)
			{
				return fP3 <= Extents.Z;
			}
			return false;
		}

		/// <summary>
		/// Serializes the FPBounds3 struct.
		/// </summary>
		/// <param name="ptr">Pointer to the FPBounds3 struct to be serialized.</param>
		/// <param name="serializer">The serializer object used to serialize and deserialize components.</param>
		public unsafe static void Serialize(void* ptr, IDeterministicFrameSerializer serializer)
		{
			FPVector3.Serialize(&((FPBounds3*)ptr)->Center, serializer);
			FPVector3.Serialize(&((FPBounds3*)ptr)->Extents, serializer);
		}

		/// <summary>
		/// Computes a hash code for the current instance of the <see cref="T:Photon.Deterministic.FPBounds3" /> class.
		/// </summary>
		/// <remarks>
		/// The hash code is computed by combining the hash codes of the <see cref="F:Photon.Deterministic.FPBounds3.Center" /> and <see cref="F:Photon.Deterministic.FPBounds3.Extents" />.
		/// </remarks>
		/// <returns>
		/// A 32-bit signed integer hash code.
		/// </returns>
		public override readonly int GetHashCode()
		{
			int num = 17;
			num = num * 31 + Center.GetHashCode();
			return num * 31 + Extents.GetHashCode();
		}
	}
}

