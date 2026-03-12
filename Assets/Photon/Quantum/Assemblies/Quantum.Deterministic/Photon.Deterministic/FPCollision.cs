using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Photon.Deterministic
{
	/// <summary>
	/// A collection of collision helper functions.
	/// </summary>
	/// \ingroup MathAPI
	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct FPCollision
	{
		private struct Box
		{
			public FPVector2 UL;

			public FPVector2 UR;

			public FPVector2 LL;

			public FPVector2 LR;

			public Box(FPVector2 center, FPVector2 extents, FP rotation)
			{
				UL.X = -extents.X;
				UL.Y = +extents.Y;
				UR.X = +extents.X;
				UR.Y = +extents.Y;
				LR.X = +extents.X;
				LR.Y = -extents.Y;
				LL.X = -extents.X;
				LL.Y = -extents.Y;
				UL = center + FPVector2.Rotate(UL, rotation);
				UR = center + FPVector2.Rotate(UR, rotation);
				LR = center + FPVector2.Rotate(LR, rotation);
				LL = center + FPVector2.Rotate(LL, rotation);
			}
		}

		private const int ClosestDistanceMaxShiftLeft = 4;

		private const int ClosestDistanceMaxShiftRight = 8;

		private const int ClosestDistanceShiftPerIterationLeft = 1;

		private const int ClosestDistanceShiftPerIterationRight = 2;

		private const long ClosestDistanceMinThresholdRaw = 8L;

		private const long ClosestDistanceMaxThresholdRaw = 2147483647L;

		/// <summary>
		/// Returns the center of a triangle defined by three vertices.
		/// </summary>
		/// <param name="v0"></param>
		/// <param name="v1"></param>
		/// <param name="v2"></param>
		/// <returns></returns>
		public static FPVector2 TriangleCenter(FPVector2 v0, FPVector2 v1, FPVector2 v2)
		{
			return FPVector2.Lerp(FPVector2.Lerp(v0, v1, FP._0_50), v2, FP._0_50);
		}

		private static bool ClosestPointOnLine(FPVector2 line_p0, FPVector2 line_p1, FPVector2 c_center, FP c_radius, out FPVector2 point)
		{
			FPVector2 a = c_center - line_p0;
			FPVector2 fPVector = c_center - line_p1;
			FPVector2 fPVector2 = line_p1 - line_p0;
			FP fP = c_radius * c_radius;
			FP sqrMagnitude = fPVector2.SqrMagnitude;
			FP fP2 = FPVector2.Dot(a, fPVector2);
			FP fP3 = fP2 / sqrMagnitude;
			if (fP3.RawValue < 0 && a.SqrMagnitude.RawValue > fP.RawValue)
			{
				point = default(FPVector2);
				return false;
			}
			if (fP3.RawValue > 65536 && fPVector.SqrMagnitude.RawValue > fP.RawValue)
			{
				point = default(FPVector2);
				return false;
			}
			point = line_p0 + fPVector2 * fP3;
			point.X = FPMath.Clamp(point.X, FPMath.Min(line_p0.X, line_p1.X), FPMath.Max(line_p0.X, line_p1.X));
			point.Y = FPMath.Clamp(point.Y, FPMath.Min(line_p0.Y, line_p1.Y), FPMath.Max(line_p0.Y, line_p1.Y));
			return true;
		}

		/// <summary>
		/// Returns <see langword="true" /> if a point <paramref name="point" /> lies on a line crossing <paramref name="p1" /> and <paramref name="p2" />.
		/// </summary>
		/// <param name="p1"></param>
		/// <param name="p2"></param>
		/// <param name="point"></param>
		/// <returns></returns>
		public static bool IsPointOnLine(FPVector2 p1, FPVector2 p2, FPVector2 point)
		{
			long num = p1.X.RawValue - p2.X.RawValue;
			if (num == 0L)
			{
				if (p1.Y.RawValue == p2.Y.RawValue)
				{
					if (point.X.RawValue == p1.X.RawValue)
					{
						return point.Y.RawValue == p1.Y.RawValue;
					}
					return false;
				}
				return point.X.RawValue == p1.X.RawValue;
			}
			long num2 = (p1.Y.RawValue - p2.Y.RawValue << 16) / num;
			long num3 = p2.Y.RawValue - (num2 * p2.X.RawValue + 32768 >> 16);
			return point.Y.RawValue == (num2 * point.X.RawValue + 32768 >> 16) + num3;
		}

		/// <summary>
		/// Returns <see langword="true" /> if a point <paramref name="point" /> lies on a segment defined by <paramref name="p1" /> and <paramref name="p2" />.
		/// </summary>
		/// <param name="p1"></param>
		/// <param name="p2"></param>
		/// <param name="point"></param>
		/// <returns></returns>
		internal static bool IsPointOnLineSegment(FPVector2 p1, FPVector2 p2, FPVector2 point)
		{
			long num = ((point.Y.RawValue - p1.Y.RawValue) * (p2.X.RawValue - p1.X.RawValue) + 32768 >> 16) - ((point.X.RawValue - p1.X.RawValue) * (p2.Y.RawValue - p1.Y.RawValue) + 32768 >> 16);
			if (num != 0L)
			{
				return false;
			}
			long num2 = ((point.X.RawValue - p1.X.RawValue) * (p2.X.RawValue - p1.X.RawValue) + 32768 >> 16) + ((point.Y.RawValue - p1.Y.RawValue) * (p2.Y.RawValue - p1.Y.RawValue) + 32768 >> 16);
			if (num2 < 0)
			{
				return false;
			}
			long num3 = ((p2.X.RawValue - p1.X.RawValue) * (p2.X.RawValue - p1.X.RawValue) + 32768 >> 16) + ((p2.Y.RawValue - p1.Y.RawValue) * (p2.Y.RawValue - p1.Y.RawValue) + 32768 >> 16);
			return num2 <= num3;
		}

		/// <summary>
		/// Returns the closest point on a segment to a given point.
		/// </summary>
		/// <param name="point">The point to find the closest point on the segment to.</param>
		/// <param name="p1">The start point of the segment.</param>
		/// <param name="p2">The end point of the segment.</param>
		/// <returns>The closest point on the segment to the given point.</returns>
		public static FPVector3 ClosestPointOnSegment(FPVector3 point, FPVector3 p1, FPVector3 p2)
		{
			long num = p1.X.RawValue - p2.X.RawValue;
			long num2 = p1.Y.RawValue - p2.Y.RawValue;
			long num3 = p1.Z.RawValue - p2.Z.RawValue;
			long num4 = (num * num + 32768 >> 16) + (num2 * num2 + 32768 >> 16) + (num3 * num3 + 32768 >> 16);
			if (num4 == 0L)
			{
				return p1;
			}
			num = (point.X.RawValue - p1.X.RawValue) * (p2.X.RawValue - p1.X.RawValue) + 32768 >> 16;
			num2 = (point.Y.RawValue - p1.Y.RawValue) * (p2.Y.RawValue - p1.Y.RawValue) + 32768 >> 16;
			num3 = (point.Z.RawValue - p1.Z.RawValue) * (p2.Z.RawValue - p1.Z.RawValue) + 32768 >> 16;
			long num5 = Math.Max(0L, Math.Min(65536L, (num + num2 + num3 << 16) / num4));
			FPVector3 result = default(FPVector3);
			result.X.RawValue = p1.X.RawValue + (num5 * (p2.X.RawValue - p1.X.RawValue) + 32768 >> 16);
			result.Y.RawValue = p1.Y.RawValue + (num5 * (p2.Y.RawValue - p1.Y.RawValue) + 32768 >> 16);
			result.Z.RawValue = p1.Z.RawValue + (num5 * (p2.Z.RawValue - p1.Z.RawValue) + 32768 >> 16);
			return result;
		}

		/// <summary>
		/// Cast point <paramref name="point" /> on a line crossing <paramref name="p1" /> and <paramref name="p2" />.
		/// The result is clamped to lie on a segment defined by <paramref name="p1" /> and <paramref name="p2" />.
		/// </summary>
		/// <param name="point"></param>
		/// <param name="p1"></param>
		/// <param name="p2"></param>
		/// <returns></returns>
		public static FPVector2 ClosestPointOnSegment(FPVector2 point, FPVector2 p1, FPVector2 p2)
		{
			long num = p1.X.RawValue - p2.X.RawValue;
			long num2 = p1.Y.RawValue - p2.Y.RawValue;
			long num3 = (num * num + 32768 >> 16) + (num2 * num2 + 32768 >> 16);
			if (num3 == 0L)
			{
				return p1;
			}
			num = (point.X.RawValue - p1.X.RawValue) * (p2.X.RawValue - p1.X.RawValue) + 32768 >> 16;
			num2 = (point.Y.RawValue - p1.Y.RawValue) * (p2.Y.RawValue - p1.Y.RawValue) + 32768 >> 16;
			long num4 = Math.Max(0L, Math.Min(65536L, (num + num2 << 16) / num3));
			FPVector2 result = default(FPVector2);
			result.X.RawValue = p1.X.RawValue + (num4 * (p2.X.RawValue - p1.X.RawValue) + 32768 >> 16);
			result.Y.RawValue = p1.Y.RawValue + (num4 * (p2.Y.RawValue - p1.Y.RawValue) + 32768 >> 16);
			return result;
		}

		/// <summary>
		/// Clamps a 2D point to an axis-aligned bounding box (AABB).
		/// </summary>
		/// <param name="point">The point to clamp.</param>
		/// <param name="boxExtents">The half extents of the AABB. The AABB is centered at the origin.</param>
		/// <param name="clampedPoint">The clamped point will be stored in this output parameter.</param>
		/// <returns>Returns <see langword="true" /> if the original point is inside the AABB, or <see langword="false" /> if it is outside. The clamped point will always be inside the AABB.</returns>
		public static bool ClampPointToAABB(FPVector2 point, FPVector2 boxExtents, out FPVector2 clampedPoint)
		{
			bool result = true;
			if (point.X < -boxExtents.X)
			{
				result = false;
				point.X = -boxExtents.X;
			}
			else if (point.X > boxExtents.X)
			{
				result = false;
				point.X = boxExtents.X;
			}
			if (point.Y < -boxExtents.Y)
			{
				result = false;
				point.Y = -boxExtents.Y;
			}
			else if (point.Y > boxExtents.Y)
			{
				result = false;
				point.Y = boxExtents.Y;
			}
			clampedPoint = point;
			return result;
		}

		/// <summary>
		/// Computes the closes point in segment A to a segment B.
		/// </summary>
		/// <param name="segment1Start">Start point of segment A.</param>
		/// <param name="segment1End">End point of segment A.</param>
		/// <param name="segment2Start">Start point of segment A.</param>
		/// <param name="segment2End">End point of segment B.</param>
		/// <returns></returns>
		public static FPVector2 ClosestPointBetweenSegments(FPVector2 segment1Start, FPVector2 segment1End, FPVector2 segment2Start, FPVector2 segment2End)
		{
			FPVector2 fPVector = segment1End - segment1Start;
			FPVector2 fPVector2 = segment2End - segment2Start;
			FPVector2 b = segment1Start - segment2Start;
			FP fP = FPVector2.Dot(fPVector, fPVector);
			FP fP2 = FPVector2.Dot(fPVector, fPVector2);
			FP fP3 = FPVector2.Dot(fPVector2, fPVector2);
			FP fP4 = FPVector2.Dot(fPVector, b);
			FP fP5 = FPVector2.Dot(fPVector2, b);
			FP fP6 = fP * fP3 - fP2 * fP2;
			FP fP7 = fP6;
			FP fP8 = fP6;
			FP fP9;
			FP fP10;
			if (fP6 < FP.Epsilon)
			{
				fP9 = FP._0;
				fP7 = FP._1;
				fP10 = fP5;
				fP8 = fP3;
			}
			else
			{
				fP9 = fP2 * fP5 - fP3 * fP4;
				fP10 = fP * fP5 - fP2 * fP4;
				if (fP9 < FP._0)
				{
					fP9 = FP._0;
					fP10 = fP5;
					fP8 = fP3;
				}
				else if (fP9 > fP7)
				{
					fP9 = fP7;
					fP10 = fP5 + fP2;
					fP8 = fP3;
				}
			}
			if (fP10 < FP._0)
			{
				fP10 = FP._0;
				if (-fP4 < FP._0)
				{
					fP9 = FP._0;
				}
				else if (-fP4 > fP)
				{
					fP9 = fP7;
				}
				else
				{
					fP9 = -fP4;
					fP7 = fP;
				}
			}
			else if (fP10 > fP8)
			{
				fP10 = fP8;
				if (-fP4 + fP2 < FP._0)
				{
					fP9 = FP._0;
				}
				else if (-fP4 + fP2 > fP)
				{
					fP9 = fP7;
				}
				else
				{
					fP9 = -fP4 + fP2;
					fP7 = fP;
				}
			}
			FP fP11 = ((FPMath.Abs(fP9) < FP.Epsilon) ? FP._0 : (fP9 / fP7));
			FP fP12 = ((FPMath.Abs(fP10) < FP.Epsilon) ? FP._0 : (fP10 / fP8));
			FPVector2 fPVector3 = segment1Start + fP11 * fPVector;
			FPVector2 b2 = segment2Start + fP12 * fPVector2;
			FP fP13 = FPVector2.Distance(fPVector3, b2);
			if (fP13 < FP.Epsilon)
			{
				return fPVector3;
			}
			FP fP14 = FPVector2.Distance(segment1Start, segment2Start);
			FP fP15 = FPVector2.Distance(segment1Start, segment2End);
			FP fP16 = FPVector2.Distance(segment1End, segment2Start);
			FP fP17 = FPVector2.Distance(segment1End, segment2End);
			FP fP18 = fP13;
			FPVector2 result = fPVector3;
			if (fP14 < fP18)
			{
				fP18 = fP14;
				result = segment1Start;
			}
			if (fP15 < fP18)
			{
				fP18 = fP15;
				result = segment1Start;
			}
			if (fP16 < fP18)
			{
				fP18 = fP16;
				result = segment1End;
			}
			if (fP17 < fP18)
			{
				result = segment1End;
			}
			return result;
		}

		/// <summary>
		/// Casts a point <paramref name="pt" /> on a triangle defined by three vertices.
		/// </summary>
		/// <param name="pt"></param>
		/// <param name="t0"></param>
		/// <param name="t1"></param>
		/// <param name="t2"></param>
		/// <returns></returns>
		public static FPVector2 ClosestPointOnTriangle(FPVector2 pt, FPVector2 t0, FPVector2 t1, FPVector2 t2)
		{
			if (TriangleContainsPointInclusive(pt, t0, t1, t2))
			{
				return pt;
			}
			FPVector2 result = ClosestPointOnSegment(pt, t0, t1);
			FPVector2 result2 = ClosestPointOnSegment(pt, t1, t2);
			FPVector2 result3 = ClosestPointOnSegment(pt, t2, t0);
			long num = result.X.RawValue - pt.X.RawValue;
			long num2 = result.Y.RawValue - pt.Y.RawValue;
			long num3 = result2.X.RawValue - pt.X.RawValue;
			long num4 = result2.Y.RawValue - pt.Y.RawValue;
			long num5 = result3.X.RawValue - pt.X.RawValue;
			long num6 = result3.Y.RawValue - pt.Y.RawValue;
			FP fP = default(FP);
			fP.RawValue = (num * num + 32768 >> 16) + (num2 * num2 + 32768 >> 16);
			FP fP2 = default(FP);
			fP2.RawValue = (num3 * num3 + 32768 >> 16) + (num4 * num4 + 32768 >> 16);
			FP fP3 = default(FP);
			fP3.RawValue = (num5 * num5 + 32768 >> 16) + (num6 * num6 + 32768 >> 16);
			if (fP < fP2 && fP < fP3)
			{
				return result;
			}
			if (fP2 < fP3)
			{
				return result2;
			}
			return result3;
		}

		/// <summary>
		/// Casts a point <paramref name="pt" /> on a circle.
		/// </summary>
		/// <param name="center"></param>
		/// <param name="radius"></param>
		/// <param name="pt"></param>
		/// <returns></returns>
		public static FPVector2 ClosestPointOnCicle(FPVector2 center, FP radius, FPVector2 pt)
		{
			FPVector2 fPVector = FPVector2.Normalize(pt - center);
			fPVector *= radius;
			return fPVector + center;
		}

		/// <summary>
		/// Checks if <paramref name="pt" /> is inside a triangle, excluding vertices and edges. Works for CW and CWW.
		/// </summary>
		/// <param name="pt">Point to check</param>
		/// <param name="v0">vertex position 0</param>
		/// <param name="v1">vertex position 1</param>
		/// <param name="v2">vertex position 2</param>
		/// <returns><see langword="true" /> if <paramref name="pt" /> is inside the triangle. <see langword="false" /> if point is outside or if the point is located on an edge or vertex.</returns>
		public static bool TriangleContainsPointExclusive(FPVector2 pt, FPVector2 v0, FPVector2 v1, FPVector2 v2)
		{
			long num = ((pt.X.RawValue - v1.X.RawValue) * (v0.Y.RawValue - v1.Y.RawValue) + 32768 >> 16) - ((v0.X.RawValue - v1.X.RawValue) * (pt.Y.RawValue - v1.Y.RawValue) + 32768 >> 16);
			long num2 = ((pt.X.RawValue - v2.X.RawValue) * (v1.Y.RawValue - v2.Y.RawValue) + 32768 >> 16) - ((v1.X.RawValue - v2.X.RawValue) * (pt.Y.RawValue - v2.Y.RawValue) + 32768 >> 16);
			long num3 = ((pt.X.RawValue - v0.X.RawValue) * (v2.Y.RawValue - v0.Y.RawValue) + 32768 >> 16) - ((v2.X.RawValue - v0.X.RawValue) * (pt.Y.RawValue - v0.Y.RawValue) + 32768 >> 16);
			bool flag = num < 0 && num2 < 0 && num3 < 0;
			bool flag2 = num > 0 && num2 > 0 && num3 > 0;
			return flag || flag2;
		}

		/// <summary>
		/// Checks if <paramref name="pt" /> is inside a triangle, excluding vertices and edges. This only checks the XZ component like the triangle is in 2D! Works for CW and CWW.
		/// </summary>
		/// <param name="pt">Point to check</param>
		/// <param name="v0">vertex position 0</param>
		/// <param name="v1">vertex position 1</param>
		/// <param name="v2">vertex position 2</param>
		/// <returns><see langword="true" /> if <paramref name="pt" /> is inside the triangle. <see langword="false" /> if point is outside or if the point is located on an edge or vertex.</returns>
		public static bool TriangleContainsPointExclusive(FPVector3 pt, FPVector3 v0, FPVector3 v1, FPVector3 v2)
		{
			long num = ((pt.X.RawValue - v1.X.RawValue) * (v0.Z.RawValue - v1.Z.RawValue) + 32768 >> 16) - ((v0.X.RawValue - v1.X.RawValue) * (pt.Z.RawValue - v1.Z.RawValue) + 32768 >> 16);
			long num2 = ((pt.X.RawValue - v2.X.RawValue) * (v1.Z.RawValue - v2.Z.RawValue) + 32768 >> 16) - ((v1.X.RawValue - v2.X.RawValue) * (pt.Z.RawValue - v2.Z.RawValue) + 32768 >> 16);
			long num3 = ((pt.X.RawValue - v0.X.RawValue) * (v2.Z.RawValue - v0.Z.RawValue) + 32768 >> 16) - ((v2.X.RawValue - v0.X.RawValue) * (pt.Z.RawValue - v0.Z.RawValue) + 32768 >> 16);
			bool flag = num < 0 && num2 < 0 && num3 < 0;
			bool flag2 = num > 0 && num2 > 0 && num3 > 0;
			return flag || flag2;
		}

		/// <summary>
		/// Checks if <paramref name="pt" /> is inside a triangle, including edges and vertices. Works for CW and CWW.
		/// </summary>
		/// <param name="pt">Point to check</param>
		/// <param name="v0">vertex position 0</param>
		/// <param name="v1">vertex position 1</param>
		/// <param name="v2">vertex position 2</param>
		/// <returns><see langword="true" /> if <paramref name="pt" /> is inside the triangle or is located on an edge or vertex.</returns>
		public static bool TriangleContainsPointInclusive(FPVector2 pt, FPVector2 v0, FPVector2 v1, FPVector2 v2)
		{
			long num = ((pt.X.RawValue - v1.X.RawValue) * (v0.Y.RawValue - v1.Y.RawValue) + 32768 >> 16) - ((v0.X.RawValue - v1.X.RawValue) * (pt.Y.RawValue - v1.Y.RawValue) + 32768 >> 16);
			long num2 = ((pt.X.RawValue - v2.X.RawValue) * (v1.Y.RawValue - v2.Y.RawValue) + 32768 >> 16) - ((v1.X.RawValue - v2.X.RawValue) * (pt.Y.RawValue - v2.Y.RawValue) + 32768 >> 16);
			long num3 = ((pt.X.RawValue - v0.X.RawValue) * (v2.Y.RawValue - v0.Y.RawValue) + 32768 >> 16) - ((v2.X.RawValue - v0.X.RawValue) * (pt.Y.RawValue - v0.Y.RawValue) + 32768 >> 16);
			bool flag = num < 0 || num2 < 0 || num3 < 0;
			bool flag2 = num > 0 || num2 > 0 || num3 > 0;
			return !(flag && flag2);
		}

		/// <summary>
		/// Checks if <paramref name="pt" /> is inside a triangle, including edges and vertices.  This only checks the XZ component like the triangle is in 2D! Works for CW and CWW.
		/// </summary>
		/// <param name="pt">Point to check</param>
		/// <param name="v0">vertex position 0</param>
		/// <param name="v1">vertex position 1</param>
		/// <param name="v2">vertex position 2</param>
		/// <returns><see langword="true" /> if <paramref name="pt" /> is inside the triangle or is located on an edge or vertex.</returns>
		public static bool TriangleContainsPointInclusive(FPVector3 pt, FPVector3 v0, FPVector3 v1, FPVector3 v2)
		{
			long num = ((pt.X.RawValue - v1.X.RawValue) * (v0.Z.RawValue - v1.Z.RawValue) + 32768 >> 16) - ((v0.X.RawValue - v1.X.RawValue) * (pt.Z.RawValue - v1.Z.RawValue) + 32768 >> 16);
			long num2 = ((pt.X.RawValue - v2.X.RawValue) * (v1.Z.RawValue - v2.Z.RawValue) + 32768 >> 16) - ((v1.X.RawValue - v2.X.RawValue) * (pt.Z.RawValue - v2.Z.RawValue) + 32768 >> 16);
			long num3 = ((pt.X.RawValue - v0.X.RawValue) * (v2.Z.RawValue - v0.Z.RawValue) + 32768 >> 16) - ((v2.X.RawValue - v0.X.RawValue) * (pt.Z.RawValue - v0.Z.RawValue) + 32768 >> 16);
			bool flag = num < 0 || num2 < 0 || num3 < 0;
			bool flag2 = num > 0 || num2 > 0 || num3 > 0;
			return !(flag && flag2);
		}

		/// <summary>
		/// Checks if <paramref name="point" /> is inside a circle, including its circumference. Works for CW and CWW.
		/// </summary>
		/// <param name="center"></param>
		/// <param name="radius"></param>
		/// <param name="point"></param>
		/// <returns><see langword="true" /> in point <paramref name="point" /> is inside the circle.</returns>
		public static bool CircleContainsPoint(FPVector2 center, FP radius, FPVector2 point)
		{
			return FPVector2.DistanceSquared(center, point).RawValue <= radius.RawValue * radius.RawValue + 32768 >> 16;
		}

		/// <summary>
		/// Circle-circle intersection test.
		/// </summary>
		/// <param name="a_origin"></param>
		/// <param name="a_radius"></param>
		/// <param name="b_origin"></param>
		/// <param name="b_radius"></param>
		/// <returns></returns>
		public static bool CircleIntersectsCircle(FPVector2 a_origin, FP a_radius, FPVector2 b_origin, FP b_radius)
		{
			long num = a_origin.X.RawValue - b_origin.X.RawValue;
			long num2 = a_origin.Y.RawValue - b_origin.Y.RawValue;
			long num3 = a_radius.RawValue + b_radius.RawValue;
			return (num * num + 32768 >> 16) + (num2 * num2 + 32768 >> 16) <= num3 * num3 + 32768 >> 16;
		}

		/// <summary>
		/// Circle-AABB intersection test.
		/// </summary>
		/// <param name="center"></param>
		/// <param name="radius"></param>
		/// <param name="min"></param>
		/// <param name="max"></param>
		/// <returns></returns>
		public static bool CircleIntersectsAABB(FPVector2 center, FP radius, FPVector2 min, FPVector2 max)
		{
			long num = ((center.X.RawValue < min.X.RawValue) ? min.X.RawValue : ((center.X.RawValue > max.X.RawValue) ? max.X.RawValue : center.X.RawValue));
			long num2 = ((center.Y.RawValue < min.Y.RawValue) ? min.Y.RawValue : ((center.Y.RawValue > max.Y.RawValue) ? max.Y.RawValue : center.Y.RawValue));
			num -= center.X.RawValue;
			num2 -= center.Y.RawValue;
			return (num * num + 32768 >> 16) + (num2 * num2 + 32768 >> 16) <= radius.RawValue * radius.RawValue + 32768 >> 16;
		}

		/// <summary>
		/// Circle-triangle intersection test.
		/// </summary>
		/// <param name="center"></param>
		/// <param name="radius"></param>
		/// <param name="v1"></param>
		/// <param name="v2"></param>
		/// <param name="v3"></param>
		/// <returns></returns>
		public static bool CircleIntersectsTriangle(FPVector2 center, FP radius, FPVector2 v1, FPVector2 v2, FPVector2 v3)
		{
			long num = radius.RawValue * radius.RawValue + 32768 >> 16;
			long num2 = center.X.RawValue - v1.X.RawValue;
			long num3 = center.Y.RawValue - v1.Y.RawValue;
			long num4 = (num2 * num2 + 32768 >> 16) + (num3 * num3 + 32768 >> 16) - num;
			if (num4 <= 0)
			{
				return true;
			}
			long num5 = center.X.RawValue - v2.X.RawValue;
			long num6 = center.Y.RawValue - v2.Y.RawValue;
			long num7 = (num5 * num5 + 32768 >> 16) + (num6 * num6 + 32768 >> 16) - num;
			if (num7 <= 0)
			{
				return true;
			}
			long num8 = center.X.RawValue - v3.X.RawValue;
			long num9 = center.Y.RawValue - v3.Y.RawValue;
			long num10 = (num8 * num8 + 32768 >> 16) + (num9 * num9 + 32768 >> 16) - num;
			if (num10 <= 0)
			{
				return true;
			}
			long num11 = v2.X.RawValue - v1.X.RawValue;
			long num12 = v2.Y.RawValue - v1.Y.RawValue;
			long num13 = v3.X.RawValue - v2.X.RawValue;
			long num14 = v3.Y.RawValue - v2.Y.RawValue;
			long num15 = v1.X.RawValue - v3.X.RawValue;
			long num16 = v1.Y.RawValue - v3.Y.RawValue;
			if (num12 * num2 + 32768 >> 16 >= num11 * num3 + 32768 >> 16 && num14 * num5 + 32768 >> 16 >= num13 * num6 + 32768 >> 16 && num16 * num8 + 32768 >> 16 >= num15 * num9 + 32768 >> 16)
			{
				return true;
			}
			long num17 = (num2 * num11 + 32768 >> 16) + (num3 * num12 + 32768 >> 16);
			if (num17 > 0)
			{
				long num18 = (num11 * num11 + 32768 >> 16) + (num12 * num12 + 32768 >> 16);
				if (num17 < num18 && num4 * num18 + 32768 >> 16 <= num17 * num17 + 32768 >> 16)
				{
					return true;
				}
			}
			num17 = (num5 * num13 + 32768 >> 16) + (num6 * num14 + 32768 >> 16);
			if (num17 > 0)
			{
				long num19 = (num13 * num13 + 32768 >> 16) + (num14 * num14 + 32768 >> 16);
				if (num17 < num19 && num7 * num19 + 32768 >> 16 <= num17 * num17 + 32768 >> 16)
				{
					return true;
				}
			}
			num17 = (num8 * num15 + 32768 >> 16) + (num9 * num16 + 32768 >> 16);
			if (num17 > 0)
			{
				long num20 = (num15 * num15 + 32768 >> 16) + (num16 * num16 + 32768 >> 16);
				if (num17 < num20 && num10 * num20 + 32768 >> 16 <= num17 * num17 + 32768 >> 16)
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Line segment-AABB intersection test.
		/// </summary>
		/// <param name="p1">First point that defines the line segment in world space.</param>
		/// <param name="p2">Second point that defines the line segment in world space.</param>
		/// <param name="aabbCenter">The center of the AABB in world space.</param>
		/// <param name="aabbExtents">The distance between the AABB center and the max X and Y values in world space.</param>
		/// <returns><see langword="true" /> if the line segment intersects the AABB and <see langword="false" /> otherwise.</returns>
		public static bool LineIntersectsAABB_SAT(FPVector2 p1, FPVector2 p2, FPVector2 aabbCenter, FPVector2 aabbExtents)
		{
			p1.X.RawValue -= aabbCenter.X.RawValue;
			p1.Y.RawValue -= aabbCenter.Y.RawValue;
			p2.X.RawValue -= aabbCenter.X.RawValue;
			p2.Y.RawValue -= aabbCenter.Y.RawValue;
			return LineIntersectsAABB_SAT(p1, p2, aabbExtents);
		}

		/// <summary>
		/// Line segment-AABB intersection test in the LOCAL space of the AABB.
		/// </summary>
		/// <param name="p1">First point that defines the line segment, relative to the AABB center.</param>
		/// <param name="p2">Second point that defines the line segment, relative to the AABB center.</param>
		/// <param name="aabbExtents">The distance between the AABB center and the max X and Y values.</param>
		/// <returns><see langword="true" /> if the line segment intersects the AABB and <see langword="false" /> otherwise.</returns>
		public static bool LineIntersectsAABB_SAT(FPVector2 p1, FPVector2 p2, FPVector2 aabbExtents)
		{
			FP x = aabbExtents.X;
			FP fP = default(FP);
			fP.RawValue = -aabbExtents.X.RawValue;
			FP x2;
			FP x3;
			if (p1.X.RawValue > p2.X.RawValue)
			{
				x2 = p1.X;
				x3 = p2.X;
			}
			else
			{
				x3 = p1.X;
				x2 = p2.X;
			}
			if (x2.RawValue < fP.RawValue || x3.RawValue > x.RawValue)
			{
				return false;
			}
			FP y = aabbExtents.Y;
			FP fP2 = default(FP);
			fP2.RawValue = -aabbExtents.Y.RawValue;
			FP y2;
			FP y3;
			if (p1.X.RawValue > p2.X.RawValue)
			{
				y2 = p1.Y;
				y3 = p2.Y;
			}
			else
			{
				y3 = p1.Y;
				y2 = p2.Y;
			}
			if (y2.RawValue < fP2.RawValue || y3.RawValue > y.RawValue)
			{
				return false;
			}
			return true;
		}

		/// <summary>
		/// Line segment-AABB intersection test in world space with computation of intersection points, normal and penetration.
		/// If an intersection is detected, the test always returns two intersection points, which can be either intersections between the line
		/// segment and an edge of the AABB or a segment point itself, if inside the AABB.
		/// </summary>
		/// <param name="p1">First point that defines the line segment in world space.</param>
		/// <param name="p2">Second point that defines the line segment in world space.</param>
		/// <param name="normal">Normal along which the line segment <paramref name="penetration" /> will be computed.</param>
		/// <param name="aabbCenter">The center of the AABB in world space.</param>
		/// <param name="aabbExtents">The distance between the AABB center and the max X and Y values in world space.</param>
		/// <param name="i1">First intersection point.</param>
		/// <param name="i2">Second intersection point.</param>
		/// <param name="penetration">The penetration of the line segment along the <paramref name="normal" />.</param>
		/// <returns><see langword="true" /> if the line segment intersects the AABB and <see langword="false" /> otherwise.</returns>
		public static bool LineIntersectsAABB2(FPVector2 p1, FPVector2 p2, FPVector2 normal, FPVector2 aabbCenter, FPVector2 aabbExtents, out FPVector2 i1, out FPVector2 i2, out FP penetration)
		{
			p1.X.RawValue -= aabbCenter.X.RawValue;
			p1.Y.RawValue -= aabbCenter.Y.RawValue;
			p2.X.RawValue -= aabbCenter.X.RawValue;
			p2.Y.RawValue -= aabbCenter.Y.RawValue;
			if (LineIntersectsAABB2(p1, p2, normal, aabbExtents, out i1, out i2, out penetration))
			{
				i1.X.RawValue += aabbCenter.X.RawValue;
				i1.Y.RawValue += aabbCenter.Y.RawValue;
				i2.X.RawValue += aabbCenter.X.RawValue;
				i2.Y.RawValue += aabbCenter.Y.RawValue;
				return true;
			}
			return false;
		}

		/// <summary>
		/// Line segment-AABB intersection test in the LOCAL space of the AABB with computation of intersection points, normal and penetration.
		/// If an intersection is detected, the test always returns two intersection points, which can be either intersections between the line
		/// segment and an edge of the AABB or a segment point itself, if inside the AABB.
		/// </summary>
		/// <param name="p1">First point that defines the line segment, relative to the AABB center.</param>
		/// <param name="p2">Second point that defines the line segment, relative to the AABB center.</param>
		/// <param name="normal">Normal along which the line segment <paramref name="penetration" /> will be computed.</param>
		/// <param name="aabbExtents">The distance between the AABB center and the max X and Y values in world space.</param>
		/// <param name="i1">First intersection point, relative to the AABB center.</param>
		/// <param name="i2">Second intersection point, relative to the AABB center.</param>
		/// <param name="penetration">The penetration of the line segment along the <paramref name="normal" />.</param>
		/// <returns><see langword="true" /> if the line segment intersects the AABB and <see langword="false" /> otherwise.</returns>
		public static bool LineIntersectsAABB2(FPVector2 p1, FPVector2 p2, FPVector2 normal, FPVector2 aabbExtents, out FPVector2 i1, out FPVector2 i2, out FP penetration)
		{
			FPVector2 fPVector = default(FPVector2);
			fPVector.X.RawValue = p2.X.RawValue - p1.X.RawValue;
			fPVector.Y.RawValue = p2.Y.RawValue - p1.Y.RawValue;
			FPVector2 fPVector2 = default(FPVector2);
			fPVector2.X.RawValue = -aabbExtents.X.RawValue - p1.X.RawValue;
			fPVector2.Y.RawValue = -aabbExtents.Y.RawValue - p1.Y.RawValue;
			FPVector2 fPVector3 = default(FPVector2);
			fPVector3.X.RawValue = aabbExtents.X.RawValue - p1.X.RawValue;
			fPVector3.Y.RawValue = aabbExtents.Y.RawValue - p1.Y.RawValue;
			long num = -2147483648L;
			long num2 = 2147483647L;
			if (fPVector.X.RawValue == 0L && (fPVector2.X.RawValue > 0 || fPVector3.X.RawValue < 0))
			{
				i1 = default(FPVector2);
				i2 = default(FPVector2);
				penetration.RawValue = 2147483647L;
				return false;
			}
			if (fPVector.Y.RawValue == 0L && (fPVector2.Y.RawValue > 0 || fPVector3.Y.RawValue < 0))
			{
				i1 = default(FPVector2);
				i2 = default(FPVector2);
				penetration.RawValue = 2147483647L;
				return false;
			}
			if (fPVector.X.RawValue != 0L)
			{
				FP fP = default(FP);
				fP.RawValue = (fPVector2.X.RawValue << 16) / fPVector.X.RawValue;
				FP fP2 = default(FP);
				fP2.RawValue = (fPVector3.X.RawValue << 16) / fPVector.X.RawValue;
				if (fP.RawValue > fP2.RawValue)
				{
					num = fP2.RawValue;
					num2 = fP.RawValue;
				}
				else
				{
					num = fP.RawValue;
					num2 = fP2.RawValue;
				}
				if (num > num2 || num2 < 0)
				{
					i1 = default(FPVector2);
					i2 = default(FPVector2);
					penetration.RawValue = 2147483647L;
					return false;
				}
			}
			if (fPVector.Y.RawValue != 0L)
			{
				FP fP3 = default(FP);
				fP3.RawValue = (fPVector2.Y.RawValue << 16) / fPVector.Y.RawValue;
				FP fP4 = default(FP);
				fP4.RawValue = (fPVector3.Y.RawValue << 16) / fPVector.Y.RawValue;
				long rawValue;
				long rawValue2;
				if (fP3.RawValue > fP4.RawValue)
				{
					rawValue = fP4.RawValue;
					rawValue2 = fP3.RawValue;
				}
				else
				{
					rawValue = fP3.RawValue;
					rawValue2 = fP4.RawValue;
				}
				if (rawValue > num)
				{
					num = rawValue;
				}
				if (rawValue2 < num2)
				{
					num2 = rawValue2;
				}
				if (num > num2 || num2 < 0)
				{
					i1 = default(FPVector2);
					i2 = default(FPVector2);
					penetration.RawValue = 2147483647L;
					return false;
				}
			}
			if (num2 > 65536 && num > 65536)
			{
				i1 = default(FPVector2);
				i2 = default(FPVector2);
				penetration.RawValue = 2147483647L;
				return false;
			}
			if (num >= 0 && num <= 65536)
			{
				i1.X.RawValue = p1.X.RawValue + (fPVector.X.RawValue * num + 32768 >> 16);
				i1.Y.RawValue = p1.Y.RawValue + (fPVector.Y.RawValue * num + 32768 >> 16);
			}
			else
			{
				i1 = p1;
			}
			if (num2 >= 0 && num2 <= 65536)
			{
				i2.X.RawValue = p1.X.RawValue + (fPVector.X.RawValue * num2 + 32768 >> 16);
				i2.Y.RawValue = p1.Y.RawValue + (fPVector.Y.RawValue * num2 + 32768 >> 16);
			}
			else
			{
				i2 = p2;
			}
			FP fP5 = default(FP);
			if (normal.X.RawValue < 0)
			{
				fP5.RawValue = -normal.X.RawValue;
			}
			else
			{
				fP5.RawValue = normal.X.RawValue;
			}
			FP fP6 = default(FP);
			if (normal.Y.RawValue < 0)
			{
				fP6.RawValue = -normal.Y.RawValue;
			}
			else
			{
				fP6.RawValue = normal.Y.RawValue;
			}
			FP fP7 = default(FP);
			FP fP8 = default(FP);
			if (fP5.RawValue > fP6.RawValue)
			{
				if (i1.X.RawValue < 0)
				{
					fP7.RawValue = aabbExtents.X.RawValue + i1.X.RawValue;
				}
				else
				{
					fP7.RawValue = aabbExtents.X.RawValue - i1.X.RawValue;
				}
				if (i2.X.RawValue < 0)
				{
					fP8.RawValue = aabbExtents.X.RawValue + i2.X.RawValue;
				}
				else
				{
					fP8.RawValue = aabbExtents.X.RawValue - i2.X.RawValue;
				}
			}
			else
			{
				if (i1.Y.RawValue < 0)
				{
					fP7.RawValue = aabbExtents.Y.RawValue + i1.Y.RawValue;
				}
				else
				{
					fP7.RawValue = aabbExtents.Y.RawValue - i1.Y.RawValue;
				}
				if (i2.Y.RawValue < 0)
				{
					fP8.RawValue = aabbExtents.Y.RawValue + i2.Y.RawValue;
				}
				else
				{
					fP8.RawValue = aabbExtents.Y.RawValue - i2.Y.RawValue;
				}
			}
			penetration = ((fP7.RawValue > fP8.RawValue) ? fP7 : fP8);
			return true;
		}

		/// <summary>
		/// Line segment-AABB intersection test in world space with computation of intersection points and penetration.
		/// </summary>
		/// <param name="p1">First point that defines the line segment in world space.</param>
		/// <param name="p2">Second point that defines the line segment in world space.</param>
		/// <param name="aabbCenter">The center of the AABB in world space.</param>
		/// <param name="aabbExtents">The distance between the AABB center and the max X and Y values in world space.</param>
		/// <param name="i1">First intersection point in world space.</param>
		/// <param name="i2">Second intersection point in world space.</param>
		/// <param name="penetration">The penetration of the line segment along the closest AABB normal.</param>
		/// <returns>
		/// The number of intersections found between the line segment and the AABB edges.
		/// If less than 2, the respective intersection point will be default.
		/// </returns>
		public static int LineIntersectsAABB(FPVector2 p1, FPVector2 p2, FPVector2 aabbCenter, FPVector2 aabbExtents, out FPVector2 i1, out FPVector2 i2, out FP penetration)
		{
			p1.X.RawValue -= aabbCenter.X.RawValue;
			p1.Y.RawValue -= aabbCenter.Y.RawValue;
			p2.X.RawValue -= aabbCenter.X.RawValue;
			p2.Y.RawValue -= aabbCenter.Y.RawValue;
			int result = LineIntersectsAABB(p1, p2, aabbExtents, out i1, out i2, out penetration);
			i1.X.RawValue += aabbCenter.X.RawValue;
			i1.Y.RawValue += aabbCenter.Y.RawValue;
			i2.X.RawValue += aabbCenter.X.RawValue;
			i2.Y.RawValue += aabbCenter.Y.RawValue;
			return result;
		}

		/// <summary>
		/// Line segment-AABB intersection test in the LOCAL space of the AABB with computation of intersection points and penetration.
		/// </summary>
		/// <param name="p1">First point that defines the line segment, relative to the AABB center.</param>
		/// <param name="p2">Second point that defines the line segment, relative to the AABB center.</param>
		/// <param name="aabbExtents">The distance between the AABB center and the max X and Y values in world space.</param>
		/// <param name="i1">First intersection point, relative to the AABB center.</param>
		/// <param name="i2">Second intersection point, relative to the AABB center.</param>
		/// <param name="penetration">The penetration of the line segment along the closest AABB normal.</param>
		/// <returns>
		/// The number of intersections found between the line segment and the AABB edges.
		/// If less than 2, the respective intersection point will be default.
		/// </returns>
		public static int LineIntersectsAABB(FPVector2 p1, FPVector2 p2, FPVector2 aabbExtents, out FPVector2 i1, out FPVector2 i2, out FP penetration)
		{
			i1 = default(FPVector2);
			i2 = default(FPVector2);
			penetration = default(FP);
			int num = 0;
			FPVector2 fPVector = default(FPVector2);
			fPVector.X = ((p1.X.RawValue < p2.X.RawValue) ? p1.X : p2.X);
			fPVector.Y = ((p1.Y.RawValue < p2.Y.RawValue) ? p1.Y : p2.Y);
			FPVector2 fPVector2 = default(FPVector2);
			fPVector2.X = ((p1.X.RawValue > p2.X.RawValue) ? p1.X : p2.X);
			fPVector2.Y = ((p1.Y.RawValue > p2.Y.RawValue) ? p1.Y : p2.Y);
			if (fPVector.X.RawValue > aabbExtents.X.RawValue || fPVector2.X.RawValue < -aabbExtents.X.RawValue)
			{
				return 0;
			}
			if (fPVector.Y.RawValue > aabbExtents.Y.RawValue || fPVector2.Y.RawValue < -aabbExtents.Y.RawValue)
			{
				return 0;
			}
			FPVector2 p3 = default(FPVector2);
			p3.X.RawValue = -aabbExtents.X.RawValue;
			p3.Y.RawValue = -aabbExtents.Y.RawValue;
			FPVector2 fPVector3 = default(FPVector2);
			fPVector3.X.RawValue = aabbExtents.X.RawValue;
			fPVector3.Y.RawValue = -aabbExtents.Y.RawValue;
			FPVector2 fPVector4 = default(FPVector2);
			fPVector4.X.RawValue = -aabbExtents.X.RawValue;
			fPVector4.Y.RawValue = aabbExtents.Y.RawValue;
			FPVector2 p4 = default(FPVector2);
			p4.X.RawValue = aabbExtents.X.RawValue;
			p4.Y.RawValue = aabbExtents.Y.RawValue;
			penetration.RawValue = 2147483647L;
			if (LineIntersectsLine(p3, fPVector3, p1, p2, out var point, out var distance))
			{
				i1 = point;
				if (distance.RawValue < penetration.RawValue)
				{
					penetration = distance;
				}
				num++;
			}
			if (LineIntersectsLine(fPVector4, p4, p1, p2, out point, out distance))
			{
				num++;
				if (distance.RawValue < penetration.RawValue)
				{
					penetration = distance;
				}
				if (num > 1)
				{
					i2 = point;
					return num;
				}
				i1 = point;
			}
			if (LineIntersectsLine(p3, fPVector4, p1, p2, out point, out distance))
			{
				num++;
				if (distance.RawValue < penetration.RawValue)
				{
					penetration = distance;
				}
				if (num > 1)
				{
					i2 = point;
					return num;
				}
				i1 = point;
			}
			if (LineIntersectsLine(fPVector3, p4, p1, p2, out point, out distance))
			{
				num++;
				if (distance.RawValue < penetration.RawValue)
				{
					penetration = distance;
				}
				if (num > 1)
				{
					i2 = point;
					return num;
				}
				i1 = point;
			}
			if (InsideAABB(p1, aabbExtents, out penetration))
			{
				point = p1;
				num++;
				if (distance.RawValue < penetration.RawValue)
				{
					penetration = distance;
				}
				if (num > 1)
				{
					i2 = point;
					return num;
				}
				i1 = point;
			}
			if (InsideAABB(p2, aabbExtents, out penetration))
			{
				point = p2;
				num++;
				if (distance.RawValue < penetration.RawValue)
				{
					penetration = distance;
				}
				if (num > 1)
				{
					i2 = point;
					return num;
				}
				i1 = point;
			}
			return num;
		}

		/// <summary>
		/// Line segment-line segment intersection test.
		/// </summary>
		/// <param name="p1"></param>
		/// <param name="p2"></param>
		/// <param name="q1"></param>
		/// <param name="q2"></param>
		/// <returns></returns>
		public static bool LineIntersectsLine(FPVector2 p1, FPVector2 p2, FPVector2 q1, FPVector2 q2)
		{
			FPVector2 fPVector = new FPVector2
			{
				X = 
				{
					RawValue = p2.X.RawValue - p1.X.RawValue
				},
				Y = 
				{
					RawValue = p2.Y.RawValue - p1.Y.RawValue
				}
			};
			FPVector2 b = new FPVector2
			{
				X = 
				{
					RawValue = q2.X.RawValue - q1.X.RawValue
				},
				Y = 
				{
					RawValue = q2.Y.RawValue - q1.Y.RawValue
				}
			};
			FPVector2 a = new FPVector2
			{
				X = 
				{
					RawValue = q1.X.RawValue - p1.X.RawValue
				},
				Y = 
				{
					RawValue = q1.Y.RawValue - p1.Y.RawValue
				}
			};
			long num = FPVector2.CrossRaw(fPVector, b);
			long num2 = FPVector2.CrossRaw(a, fPVector);
			if (num == 0L && num2 == 0L)
			{
				return false;
			}
			if (num == 0L && num2 != 0L)
			{
				return false;
			}
			long num3 = (FPVector2.CrossRaw(a, b) << 16) / num;
			long num4 = (num2 << 16) / num;
			if (num != 0L && 0 <= num3 && num3 <= 65536)
			{
				if (0 <= num4)
				{
					return num4 <= 65536;
				}
				return false;
			}
			return false;
		}

		/// <summary>
		/// Line segment-line segment intersection test. 
		/// </summary>
		/// <param name="p1"></param>
		/// <param name="p2"></param>
		/// <param name="q1"></param>
		/// <param name="q2"></param>
		/// <param name="point">Point of collision</param>
		/// <param name="distance">Distance along p segment where the collision happens</param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool LineIntersectsLine(FPVector2 p1, FPVector2 p2, FPVector2 q1, FPVector2 q2, out FPVector2 point, out FP distance)
		{
			FP normalizedDist;
			return LineIntersectsLine(p1, p2, q1, q2, out point, out distance, out normalizedDist);
		}

		/// <summary>
		/// Determines if two lines intersect.
		/// </summary>
		/// <param name="p1">The starting point of the first line.</param>
		/// <param name="p2">The ending point of the first line.</param>
		/// <param name="q1">The starting point of the second line.</param>
		/// <param name="q2">The ending point of the second line.</param>
		/// <param name="point">The intersection point, if the lines intersect.</param>
		/// <param name="distance">The distance between the intersection point and point p1, if the lines intersect.</param>
		/// <param name="normalizedDist">The normalized distance between the intersection point and point p1, if the lines intersect.</param>
		/// <returns><see langword="true" /> if the lines intersect, <see langword="false" /> otherwise.</returns>
		public static bool LineIntersectsLine(FPVector2 p1, FPVector2 p2, FPVector2 q1, FPVector2 q2, out FPVector2 point, out FP distance, out FP normalizedDist)
		{
			FPVector2 fPVector = new FPVector2
			{
				X = 
				{
					RawValue = p2.X.RawValue - p1.X.RawValue
				},
				Y = 
				{
					RawValue = p2.Y.RawValue - p1.Y.RawValue
				}
			};
			FPVector2 b = new FPVector2
			{
				X = 
				{
					RawValue = q2.X.RawValue - q1.X.RawValue
				},
				Y = 
				{
					RawValue = q2.Y.RawValue - q1.Y.RawValue
				}
			};
			FPVector2 a = new FPVector2
			{
				X = 
				{
					RawValue = q1.X.RawValue - p1.X.RawValue
				},
				Y = 
				{
					RawValue = q1.Y.RawValue - p1.Y.RawValue
				}
			};
			long num = FPVector2.CrossRaw(fPVector, b);
			long num2 = FPVector2.CrossRaw(a, fPVector);
			if (num == 0L && num2 == 0L)
			{
				point = default(FPVector2);
				distance = default(FP);
				normalizedDist = default(FP);
				return false;
			}
			if (num == 0L && num2 != 0L)
			{
				point = default(FPVector2);
				distance = default(FP);
				normalizedDist = default(FP);
				return false;
			}
			normalizedDist.RawValue = (FPVector2.CrossRaw(a, b) << 16) / num;
			long num3 = (num2 << 16) / num;
			if (num != 0L && 0 <= normalizedDist.RawValue && normalizedDist.RawValue <= 65536 && 0 <= num3 && num3 <= 65536)
			{
				fPVector.X.RawValue = fPVector.X.RawValue * normalizedDist.RawValue + 32768 >> 16;
				fPVector.Y.RawValue = fPVector.Y.RawValue * normalizedDist.RawValue + 32768 >> 16;
				distance = fPVector.Magnitude;
				fPVector.X.RawValue += p1.X.RawValue;
				fPVector.Y.RawValue += p1.Y.RawValue;
				point = fPVector;
				return true;
			}
			point = default(FPVector2);
			distance = default(FP);
			normalizedDist = default(FP);
			return false;
		}

		/// <summary>
		/// Line segment-line segment intersection test. 
		/// </summary>
		/// <param name="p1"></param>
		/// <param name="p2"></param>
		/// <param name="q1"></param>
		/// <param name="q2"></param>
		/// <param name="point">Point of collision</param>
		/// <returns></returns>
		public static bool LineIntersectsLine(FPVector2 p1, FPVector2 p2, FPVector2 q1, FPVector2 q2, out FPVector2 point)
		{
			FPVector2 fPVector = new FPVector2
			{
				X = 
				{
					RawValue = p2.X.RawValue - p1.X.RawValue
				},
				Y = 
				{
					RawValue = p2.Y.RawValue - p1.Y.RawValue
				}
			};
			FPVector2 b = new FPVector2
			{
				X = 
				{
					RawValue = q2.X.RawValue - q1.X.RawValue
				},
				Y = 
				{
					RawValue = q2.Y.RawValue - q1.Y.RawValue
				}
			};
			FPVector2 a = new FPVector2
			{
				X = 
				{
					RawValue = q1.X.RawValue - p1.X.RawValue
				},
				Y = 
				{
					RawValue = q1.Y.RawValue - p1.Y.RawValue
				}
			};
			long num = FPVector2.CrossRaw(fPVector, b);
			long num2 = FPVector2.CrossRaw(a, fPVector);
			if (num == 0L && num2 == 0L)
			{
				point = default(FPVector2);
				return false;
			}
			if (num == 0L && num2 != 0L)
			{
				point = default(FPVector2);
				return false;
			}
			long num3 = (FPVector2.CrossRaw(a, b) << 16) / num;
			long num4 = (num2 << 16) / num;
			if (num != 0L && 0 <= num3 && num3 <= 65536 && 0 <= num4 && num4 <= 65536)
			{
				fPVector.X.RawValue = fPVector.X.RawValue * num3 + 32768 >> 16;
				fPVector.Y.RawValue = fPVector.Y.RawValue * num3 + 32768 >> 16;
				fPVector.X.RawValue += p1.X.RawValue;
				fPVector.Y.RawValue += p1.Y.RawValue;
				point = fPVector;
				return true;
			}
			point = default(FPVector2);
			return false;
		}

		/// <summary>
		/// Line segment-line segment intersection test. Assumes lines are not colinear nor parallel.
		/// </summary>
		/// <param name="p1"></param>
		/// <param name="p2"></param>
		/// <param name="q1"></param>
		/// <param name="q2"></param>
		/// <param name="point"></param>
		public static void LineIntersectsLineAlwaysHit(FPVector2 p1, FPVector2 p2, FPVector2 q1, FPVector2 q2, out FPVector2 point)
		{
			FPVector2 fPVector = new FPVector2
			{
				X = 
				{
					RawValue = p2.X.RawValue - p1.X.RawValue
				},
				Y = 
				{
					RawValue = p2.Y.RawValue - p1.Y.RawValue
				}
			};
			FPVector2 b = new FPVector2
			{
				X = 
				{
					RawValue = q2.X.RawValue - q1.X.RawValue
				},
				Y = 
				{
					RawValue = q2.Y.RawValue - q1.Y.RawValue
				}
			};
			FPVector2 a = new FPVector2
			{
				X = 
				{
					RawValue = q1.X.RawValue - p1.X.RawValue
				},
				Y = 
				{
					RawValue = q1.Y.RawValue - p1.Y.RawValue
				}
			};
			long num = FPVector2.CrossRaw(fPVector, b);
			long num2 = (FPVector2.CrossRaw(a, b) << 16) / num;
			fPVector.X.RawValue = fPVector.X.RawValue * num2 + 32768 >> 16;
			fPVector.Y.RawValue = fPVector.Y.RawValue * num2 + 32768 >> 16;
			fPVector.X.RawValue += p1.X.RawValue;
			fPVector.Y.RawValue += p1.Y.RawValue;
			point = fPVector;
		}

		/// <summary>
		/// Returns <see langword="true" /> if <paramref name="point" /> is inside centered AABB.
		/// </summary>
		/// <param name="point"></param>
		/// <param name="extents"></param>
		/// <param name="penetration"></param>
		/// <returns></returns>
		public static bool InsideAABB(FPVector2 point, FPVector2 extents, out FP penetration)
		{
			FP fP = extents.X - point.X;
			FP fP2 = extents.X + point.X;
			FP fP3 = extents.Y - point.Y;
			FP fP4 = extents.Y + point.Y;
			if (fP >= 0 && fP2 >= 0 && fP3 >= 0 && fP4 >= 0)
			{
				penetration = FPMath.Min(FPMath.Min(fP, fP2), FPMath.Min(fP3, fP4));
				return true;
			}
			penetration = FP.UseableMax;
			return false;
		}

		/// <summary>
		/// Line segment-circle intersection test.
		/// </summary>
		/// <param name="p1"></param>
		/// <param name="p2"></param>
		/// <param name="position"></param>
		/// <param name="radius"></param>
		/// <param name="point"></param>
		/// <returns></returns>
		public static bool LineIntersectsCircleManifold(FPVector2 p1, FPVector2 p2, FPVector2 position, FP radius, out FPVector2 point)
		{
			point = default(FPVector2);
			long num = position.X.RawValue - p1.X.RawValue;
			long num2 = position.Y.RawValue - p1.Y.RawValue;
			long num3 = p2.X.RawValue - p1.X.RawValue;
			long num4 = p2.Y.RawValue - p1.Y.RawValue;
			long x = (num3 * num3 + 32768 >> 16) + (num4 * num4 + 32768 >> 16);
			x = FPMath.SqrtRaw(x);
			long num5 = (num3 << 16) / x;
			long num6 = (num4 << 16) / x;
			long num7 = (num5 * num + 32768 >> 16) + (num6 * num2 + 32768 >> 16);
			long num8 = 0L;
			if (num7 < 0)
			{
				num8 = ((p1.X.RawValue - position.X.RawValue) * (p1.X.RawValue - position.X.RawValue) + 32768 >> 16) + ((p1.Y.RawValue - position.Y.RawValue) * (p1.Y.RawValue - position.Y.RawValue) + 32768 >> 16);
				num8 = FPMath.SqrtRaw(num8);
				if (num8 <= radius.RawValue)
				{
					point.X.RawValue = p1.X.RawValue;
					point.Y.RawValue = p1.Y.RawValue;
					return true;
				}
				return false;
			}
			if (num7 > x)
			{
				num8 = ((p2.X.RawValue - position.X.RawValue) * (p2.X.RawValue - position.X.RawValue) + 32768 >> 16) + ((p2.Y.RawValue - position.Y.RawValue) * (p2.Y.RawValue - position.Y.RawValue) + 32768 >> 16);
				num8 = FPMath.SqrtRaw(num8);
				if (num8 <= radius.RawValue)
				{
					point.X.RawValue = p2.X.RawValue;
					point.Y.RawValue = p2.Y.RawValue;
					return true;
				}
				return false;
			}
			long num9 = p1.X.RawValue + (num5 * num7 + 32768 >> 16);
			long num10 = p1.Y.RawValue + (num6 * num7 + 32768 >> 16);
			num8 = ((num9 - position.X.RawValue) * (num9 - position.X.RawValue) + 32768 >> 16) + ((num10 - position.Y.RawValue) * (num10 - position.Y.RawValue) + 32768 >> 16);
			num8 = FPMath.SqrtRaw(num8);
			if (num8 <= radius.RawValue)
			{
				point.X.RawValue = num9;
				point.Y.RawValue = num10;
				return true;
			}
			return false;
		}

		/// <summary>
		/// Line segment-circle intersection test.
		/// </summary>
		/// <param name="p1">Start point of the line segment.</param>
		/// <param name="p2">End point of the line segment.</param>
		/// <param name="position">Position of the center of the circle in world space.</param>
		/// <param name="radius">Radius of the circle.</param>
		/// <param name="ignoreIfStartPointInside">If the intersection should be ignored if the start point of the line segment (<paramref name="p1" />) is inside the circle.</param>
		/// <returns><see langword="true" /> if an intersection is detected. <see langword="false" /> otherwise.</returns>
		public static bool LineIntersectsCircle(FPVector2 p1, FPVector2 p2, FPVector2 position, FP radius, bool ignoreIfStartPointInside = false)
		{
			FPVector2 point;
			FP normalizedDist;
			return LineIntersectsCircle(p1, p2, position, radius, out point, out normalizedDist, ignoreIfStartPointInside);
		}

		/// <summary>
		/// Line segment-circle intersection test.
		/// </summary>
		/// <param name="p1">Start point of the line segment.</param>
		/// <param name="p2">End point of the line segment.</param>
		/// <param name="position">Position of the center of the circle in world space.</param>
		/// <param name="radius">Radius of the circle.</param>
		/// <param name="point">Intersection point, if intersecting. Default otherwise.</param>
		/// <param name="ignoreIfStartPointInside">If the intersection should be ignored if the start point of the line segment (<paramref name="p1" />) is inside the circle.</param>
		/// <returns><see langword="true" /> if an intersection is detected. <see langword="false" /> otherwise.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool LineIntersectsCircle(FPVector2 p1, FPVector2 p2, FPVector2 position, FP radius, out FPVector2 point, bool ignoreIfStartPointInside = false)
		{
			FP normalizedDist;
			return LineIntersectsCircle(p1, p2, position, radius, out point, out normalizedDist, ignoreIfStartPointInside);
		}

		/// <summary>
		/// Line segment-circle intersection test.
		/// </summary>
		/// <param name="p1">Start point of the line segment.</param>
		/// <param name="p2">End point of the line segment.</param>
		/// <param name="position">Position of the center of the circle in world space.</param>
		/// <param name="radius">Radius of the circle.</param>
		/// <param name="point">Intersection point, if intersecting. Default otherwise.</param>
		/// <param name="normalizedDist">Normalize distance from <paramref name="p1" /> to <paramref name="p2" /> of the intersection point, if intersecting. Default otherwise.</param>
		/// <param name="ignoreIfStartPointInside">If the intersection should be ignored if the start point of the line segment (<paramref name="p1" />) is inside the circle.</param>
		/// <returns><see langword="true" /> if an intersection is detected. <see langword="false" /> otherwise.</returns>
		public static bool LineIntersectsCircle(FPVector2 p1, FPVector2 p2, FPVector2 position, FP radius, out FPVector2 point, out FP normalizedDist, bool ignoreIfStartPointInside = false)
		{
			FPVector2 value = default(FPVector2);
			value.X.RawValue = p2.X.RawValue - p1.X.RawValue;
			value.Y.RawValue = p2.Y.RawValue - p1.Y.RawValue;
			value = FPVector2.Normalize(value, out var magnitude);
			FP fP = default(FP);
			fP.RawValue = radius.RawValue * radius.RawValue + 32768 >> 16;
			if (magnitude.RawValue == 0L || fP.RawValue == 0L)
			{
				point = default(FPVector2);
				normalizedDist = default(FP);
				return false;
			}
			FPVector2 value2 = default(FPVector2);
			value2.X.RawValue = p1.X.RawValue - position.X.RawValue;
			value2.Y.RawValue = p1.Y.RawValue - position.Y.RawValue;
			value2 = FPVector2.Normalize(value2, out var magnitude2);
			FP fP2 = default(FP);
			fP2.RawValue = (value2.X.RawValue * value.X.RawValue + 32768 >> 16) + (value2.Y.RawValue * value.Y.RawValue + 32768 >> 16);
			FP fP3 = default(FP);
			fP3.RawValue = 65536 - (fP2.RawValue * fP2.RawValue + 32768 >> 16);
			FP fP4 = default(FP);
			if (fP3.RawValue > 0)
			{
				fP4.RawValue = magnitude2.RawValue * FPMath.SqrtRaw(fP3.RawValue) + 32768 >> 16;
			}
			else
			{
				fP4 = default(FP);
			}
			if (fP4.RawValue > radius.RawValue)
			{
				point = default(FPVector2);
				normalizedDist = default(FP);
				return false;
			}
			FP fP5 = default(FP);
			fP5.RawValue = FPMath.SqrtRaw(fP.RawValue - (fP4.RawValue * fP4.RawValue + 32768 >> 16));
			FP fP6 = default(FP);
			fP6.RawValue = -fP2.RawValue * magnitude2.RawValue + 32768 >> 16;
			FP fP7 = default(FP);
			fP7.RawValue = fP6.RawValue - fP5.RawValue;
			if (0 <= fP7.RawValue && fP7.RawValue <= magnitude.RawValue)
			{
				normalizedDist.RawValue = (fP7.RawValue << 16) / magnitude.RawValue;
				point.X.RawValue = p1.X.RawValue + (value.X.RawValue * fP7.RawValue + 32768 >> 16);
				point.Y.RawValue = p1.Y.RawValue + (value.Y.RawValue * fP7.RawValue + 32768 >> 16);
				return true;
			}
			if (!ignoreIfStartPointInside)
			{
				fP7.RawValue = fP6.RawValue + fP5.RawValue;
				if (0 <= fP7.RawValue && fP7.RawValue <= magnitude.RawValue)
				{
					normalizedDist.RawValue = (fP7.RawValue << 16) / magnitude.RawValue;
					point.X.RawValue = p1.X.RawValue + (value.X.RawValue * fP7.RawValue + 32768 >> 16);
					point.Y.RawValue = p1.Y.RawValue + (value.Y.RawValue * fP7.RawValue + 32768 >> 16);
					return true;
				}
			}
			point = default(FPVector2);
			normalizedDist = default(FP);
			return false;
		}

		/// <summary>
		/// Circle-polygon intersection test.
		/// </summary>
		/// <param name="circleCenter"></param>
		/// <param name="circleRadius"></param>
		/// <param name="polygonPosition"></param>
		/// <param name="polygonRotationSinInverse"></param>
		/// <param name="polygonRotationCosInverse"></param>
		/// <param name="polygonVertices"></param>
		/// <param name="polygonNormals"></param>
		/// <returns></returns>
		public static bool CircleIntersectsPolygon(FPVector2 circleCenter, FP circleRadius, FPVector2 polygonPosition, FP polygonRotationSinInverse, FP polygonRotationCosInverse, FPVector2[] polygonVertices, FPVector2[] polygonNormals)
		{
			FPVector2 fPVector = FPVector2.Rotate(circleCenter - polygonPosition, polygonRotationSinInverse, polygonRotationCosInverse);
			FP fP = -FP.MaxValue;
			int num = 0;
			for (int i = 0; i < polygonVertices.Length; i++)
			{
				FP fP2 = FPVector2.Dot(polygonNormals[i], fPVector - polygonVertices[i]);
				if (fP2 > circleRadius)
				{
					return false;
				}
				if (fP2 > fP)
				{
					fP = fP2;
					num = i;
				}
			}
			if (fP < FP.Epsilon)
			{
				return true;
			}
			FPVector2 fPVector2 = polygonVertices[num];
			FPVector2 fPVector3 = polygonVertices[(num + 1) % polygonVertices.Length];
			FP fP3 = FPVector2.Dot(fPVector - fPVector2, fPVector3 - fPVector2);
			FP fP4 = FPVector2.Dot(fPVector - fPVector3, fPVector2 - fPVector3);
			FP fP5 = circleRadius * circleRadius;
			if (fP3 <= FP._0)
			{
				return !(FPVector2.DistanceSquared(fPVector, fPVector2) > fP5);
			}
			if (fP4 <= FP._0)
			{
				return !(FPVector2.DistanceSquared(fPVector, fPVector3) > fP5);
			}
			return !(FPVector2.Dot(fPVector - fPVector2, polygonNormals[num]) > circleRadius);
		}

		/// <summary>
		/// Circle-polygon intersection test.
		/// </summary>
		/// <param name="circleCenter"></param>
		/// <param name="circleRadius"></param>
		/// <param name="polygonPosition"></param>
		/// <param name="polygonRotation"></param>
		/// <param name="polygonVertices"></param>
		/// <param name="polygonNormals"></param>
		/// <returns></returns>
		public static bool CircleIntersectsPolygon(FPVector2 circleCenter, FP circleRadius, FPVector2 polygonPosition, FP polygonRotation, FPVector2[] polygonVertices, FPVector2[] polygonNormals)
		{
			FPMath.SinCos(-polygonRotation, out var sin, out var cos);
			return CircleIntersectsPolygon(circleCenter, circleRadius, polygonPosition, sin, cos, polygonVertices, polygonNormals);
		}

		/// <summary>
		/// Box-Box (2D) intersection test.
		/// </summary>
		/// <param name="aCenter"></param>
		/// <param name="aExtents"></param>
		/// <param name="aRotation"></param>
		/// <param name="bCenter"></param>
		/// <param name="bExtents"></param>
		/// <param name="bRotation"></param>
		/// <returns></returns>
		public static bool BoxIntersectsBox(FPVector2 aCenter, FPVector2 aExtents, FP aRotation, FPVector2 bCenter, FPVector2 bExtents, FP bRotation)
		{
			Box a = new Box(aCenter, aExtents, aRotation);
			Box b = new Box(bCenter, bExtents, bRotation);
			bool flag = Project(new FPVector2(a.UR.X - a.UL.X, a.UR.Y - a.UL.Y), a, b);
			bool flag2 = Project(new FPVector2(a.UR.X - a.LR.X, a.UR.Y - a.LR.Y), a, b);
			bool flag3 = Project(new FPVector2(b.UL.X - b.LL.X, b.UL.Y - b.LL.Y), a, b);
			bool flag4 = Project(new FPVector2(b.UL.X - b.UR.X, b.UL.Y - b.UR.Y), a, b);
			return flag && flag2 && flag3 && flag4;
		}

		private static bool Project(FPVector2 axis, Box a, Box b)
		{
			a.UL = Project(axis, a.UL);
			a.UR = Project(axis, a.UR);
			a.LL = Project(axis, a.LL);
			a.LR = Project(axis, a.LR);
			b.UL = Project(axis, b.UL);
			b.UR = Project(axis, b.UR);
			b.LL = Project(axis, b.LL);
			b.LR = Project(axis, b.LR);
			FP maxValue = FP.MaxValue;
			FP minValue = FP.MinValue;
			FP maxValue2 = FP.MaxValue;
			FP minValue2 = FP.MinValue;
			FP val = FPVector2.Dot(axis, a.UL);
			maxValue = FPMath.Min(val, maxValue);
			minValue = FPMath.Max(val, minValue);
			val = FPVector2.Dot(axis, a.UR);
			maxValue = FPMath.Min(val, maxValue);
			minValue = FPMath.Max(val, minValue);
			val = FPVector2.Dot(axis, a.LL);
			maxValue = FPMath.Min(val, maxValue);
			minValue = FPMath.Max(val, minValue);
			val = FPVector2.Dot(axis, a.LR);
			maxValue = FPMath.Min(val, maxValue);
			minValue = FPMath.Max(val, minValue);
			val = FPVector2.Dot(axis, b.UL);
			maxValue2 = FPMath.Min(val, maxValue2);
			minValue2 = FPMath.Max(val, minValue2);
			val = FPVector2.Dot(axis, b.UR);
			maxValue2 = FPMath.Min(val, maxValue2);
			minValue2 = FPMath.Max(val, minValue2);
			val = FPVector2.Dot(axis, b.LL);
			maxValue2 = FPMath.Min(val, maxValue2);
			minValue2 = FPMath.Max(val, minValue2);
			val = FPVector2.Dot(axis, b.LR);
			maxValue2 = FPMath.Min(val, maxValue2);
			minValue2 = FPMath.Max(val, minValue2);
			if (maxValue2 <= minValue)
			{
				return minValue2 >= maxValue;
			}
			return false;
		}

		private static FPVector2 Project(FPVector2 axis, FPVector2 point)
		{
			FP fP = point.X * axis.X + point.Y * axis.Y;
			FP fP2 = axis.X * axis.X + axis.Y * axis.Y;
			return new FPVector2(fP / fP2 * axis.X, fP / fP2 * axis.Y);
		}

		/// <summary>
		/// Uses barycentric coordinates to calculate the closest point on a triangle. In conjunction with Fixed Point math this can get quite inaccurate when the triangle become large (more than 100 units) or tiny (less then 0.01 units).
		/// </summary>
		/// <param name="p">Point</param>
		/// <param name="a">Vertex 0</param>
		/// <param name="b">Vertex 1</param>
		/// <param name="c">Vertex 2</param>
		/// <param name="closestPoint">Resulting point on the triangle</param>
		/// <param name="barycentricCoordinates">Barycentric coordinates of the point inside the triangle.</param>
		/// <returns>Squared distance to point on the triangle.</returns>
		public static FP ClosestDistanceToTriangle(FPVector3 p, FPVector3 a, FPVector3 b, FPVector3 c, out FPVector3 closestPoint, out FPVector3 barycentricCoordinates)
		{
			barycentricCoordinates.X.RawValue = 0L;
			barycentricCoordinates.Y.RawValue = 0L;
			barycentricCoordinates.Z.RawValue = 0L;
			long rawValue = a.X.RawValue;
			long rawValue2 = a.Y.RawValue;
			long rawValue3 = a.Z.RawValue;
			long rawValue4 = b.X.RawValue;
			long rawValue5 = b.Y.RawValue;
			long rawValue6 = b.Z.RawValue;
			long rawValue7 = c.X.RawValue;
			long rawValue8 = c.Y.RawValue;
			long rawValue9 = c.Z.RawValue;
			rawValue -= p.X.RawValue;
			rawValue2 -= p.Y.RawValue;
			rawValue3 -= p.Z.RawValue;
			rawValue4 -= p.X.RawValue;
			rawValue5 -= p.Y.RawValue;
			rawValue6 -= p.Z.RawValue;
			rawValue7 -= p.X.RawValue;
			rawValue8 -= p.Y.RawValue;
			rawValue9 -= p.Z.RawValue;
			long num = rawValue4 - rawValue;
			long num2 = rawValue5 - rawValue2;
			long num3 = rawValue6 - rawValue3;
			long num4 = rawValue7 - rawValue;
			long num5 = rawValue8 - rawValue2;
			long num6 = rawValue9 - rawValue3;
			long num7 = (num2 * num6 + 32768 >> 16) - (num3 * num5 + 32768 >> 16);
			long num8 = (num3 * num4 + 32768 >> 16) - (num * num6 + 32768 >> 16);
			long num9 = (num * num5 + 32768 >> 16) - (num2 * num4 + 32768 >> 16);
			long value = (num7 * num7 >> 16) + (num8 * num8 >> 16) + (num9 * num9 >> 16);
			int num10 = 0;
			while ((Math.Abs(num7) < 8 || Math.Abs(num8) < 8 || Math.Abs(num9) < 8 || Math.Abs(value) < 8) && num10 < 4)
			{
				num10++;
				rawValue <<= 1;
				rawValue2 <<= 1;
				rawValue3 <<= 1;
				rawValue4 <<= 1;
				rawValue5 <<= 1;
				rawValue6 <<= 1;
				rawValue7 <<= 1;
				rawValue8 <<= 1;
				rawValue9 <<= 1;
				num = rawValue4 - rawValue;
				num2 = rawValue5 - rawValue2;
				num3 = rawValue6 - rawValue3;
				num4 = rawValue7 - rawValue;
				num5 = rawValue8 - rawValue2;
				num6 = rawValue9 - rawValue3;
				num7 = (num2 * num6 + 32768 >> 16) - (num3 * num5 + 32768 >> 16);
				num8 = (num3 * num4 + 32768 >> 16) - (num * num6 + 32768 >> 16);
				num9 = (num * num5 + 32768 >> 16) - (num2 * num4 + 32768 >> 16);
				value = (num7 * num7 >> 16) + (num8 * num8 >> 16) + (num9 * num9 >> 16);
			}
			while ((Math.Abs(num7) > FP.UseableMax.RawValue || Math.Abs(num8) > FP.UseableMax.RawValue || Math.Abs(num9) > FP.UseableMax.RawValue || Math.Abs(value) > int.MaxValue) && -num10 < 8)
			{
				num10 -= 2;
				rawValue >>= 2;
				rawValue2 >>= 2;
				rawValue3 >>= 2;
				rawValue4 >>= 2;
				rawValue5 >>= 2;
				rawValue6 >>= 2;
				rawValue7 >>= 2;
				rawValue8 >>= 2;
				rawValue9 >>= 2;
				num = rawValue4 - rawValue;
				num2 = rawValue5 - rawValue2;
				num3 = rawValue6 - rawValue3;
				num4 = rawValue7 - rawValue;
				num5 = rawValue8 - rawValue2;
				num6 = rawValue9 - rawValue3;
				num7 = (num2 * num6 + 32768 >> 16) - (num3 * num5 + 32768 >> 16);
				num8 = (num3 * num4 + 32768 >> 16) - (num * num6 + 32768 >> 16);
				num9 = (num * num5 + 32768 >> 16) - (num2 * num4 + 32768 >> 16);
				value = (num7 * num7 >> 16) + (num8 * num8 >> 16) + (num9 * num9 >> 16);
			}
			FP fP = default(FP);
			fP.RawValue = (num * rawValue7 + 32768 >> 16) + (num2 * rawValue8 + 32768 >> 16) + (num3 * rawValue9 + 32768 >> 16);
			FP fP2 = default(FP);
			fP2.RawValue = (num4 * rawValue7 + 32768 >> 16) + (num5 * rawValue8 + 32768 >> 16) + (num6 * rawValue9 + 32768 >> 16);
			if (fP2.RawValue <= 0 && fP.RawValue >= fP2.RawValue)
			{
				barycentricCoordinates.Z.RawValue = 65536L;
				closestPoint = c;
			}
			else
			{
				FP fP3 = default(FP);
				fP3.RawValue = (num * rawValue + 32768 >> 16) + (num2 * rawValue2 + 32768 >> 16) + (num3 * rawValue3 + 32768 >> 16);
				FP fP4 = default(FP);
				fP4.RawValue = (num4 * rawValue + 32768 >> 16) + (num5 * rawValue2 + 32768 >> 16) + (num6 * rawValue3 + 32768 >> 16);
				if (fP3.RawValue >= 0 && fP4.RawValue >= 0)
				{
					barycentricCoordinates.X.RawValue = 65536L;
					closestPoint = a;
				}
				else
				{
					FP fP5 = default(FP);
					fP5.RawValue = (fP.RawValue * fP4.RawValue + 32768 >> 16) - (fP3.RawValue * fP2.RawValue + 32768 >> 16);
					if (fP5.RawValue <= 0 && fP4.RawValue <= 0 && fP2.RawValue >= 0)
					{
						long num11;
						long num12;
						if (fP4.RawValue == fP2.RawValue)
						{
							num11 = 32768L;
							num12 = 32768L;
						}
						else
						{
							num12 = (fP4.RawValue << 16) / (fP4.RawValue - fP2.RawValue);
							num11 = 65536 - num12;
						}
						closestPoint.X.RawValue = num11 * a.X.RawValue + num12 * c.X.RawValue + 32768 >> 16;
						closestPoint.Y.RawValue = num11 * a.Y.RawValue + num12 * c.Y.RawValue + 32768 >> 16;
						closestPoint.Z.RawValue = num11 * a.Z.RawValue + num12 * c.Z.RawValue + 32768 >> 16;
						barycentricCoordinates.X.RawValue = num11;
						barycentricCoordinates.Z.RawValue = num12;
					}
					else
					{
						FP fP6 = default(FP);
						fP6.RawValue = (num * rawValue4 + 32768 >> 16) + (num2 * rawValue5 + 32768 >> 16) + (num3 * rawValue6 + 32768 >> 16);
						FP fP7 = default(FP);
						fP7.RawValue = (num4 * rawValue4 + 32768 >> 16) + (num5 * rawValue5 + 32768 >> 16) + (num6 * rawValue6 + 32768 >> 16);
						if (fP6.RawValue <= 0 && fP7.RawValue >= fP6.RawValue)
						{
							barycentricCoordinates.Y.RawValue = 65536L;
							closestPoint = b;
						}
						else
						{
							FP fP8 = default(FP);
							fP8.RawValue = (fP6.RawValue * fP2.RawValue + 32768 >> 16) - (fP.RawValue * fP7.RawValue + 32768 >> 16);
							if (fP8.RawValue <= 0 && fP6.RawValue >= fP7.RawValue && fP2.RawValue >= fP.RawValue)
							{
								FP fP9 = default(FP);
								fP9.RawValue = fP6.RawValue - fP7.RawValue + (fP2.RawValue - fP.RawValue);
								long num12;
								long num13;
								if (fP9.RawValue == 0L)
								{
									num12 = 32768L;
									num13 = 32768L;
								}
								else
								{
									num12 = (fP6.RawValue - fP7.RawValue << 16) / fP9.RawValue;
									num13 = 65536 - num12;
								}
								closestPoint.X.RawValue = num12 * c.X.RawValue + num13 * b.X.RawValue + 32768 >> 16;
								closestPoint.Y.RawValue = num12 * c.Y.RawValue + num13 * b.Y.RawValue + 32768 >> 16;
								closestPoint.Z.RawValue = num12 * c.Z.RawValue + num13 * b.Z.RawValue + 32768 >> 16;
								barycentricCoordinates.Y.RawValue = num13;
								barycentricCoordinates.Z.RawValue = num12;
							}
							else
							{
								FP fP10 = default(FP);
								fP10.RawValue = (fP3.RawValue * fP7.RawValue + 32768 >> 16) - (fP6.RawValue * fP4.RawValue + 32768 >> 16);
								if (fP10.RawValue <= 0 && fP3.RawValue <= 0 && fP6.RawValue >= 0)
								{
									long num11;
									long num13;
									if (fP3.RawValue == fP6.RawValue)
									{
										num11 = 32768L;
										num13 = 32768L;
									}
									else
									{
										num13 = (fP3.RawValue << 16) / (fP3.RawValue - fP6.RawValue);
										num11 = 65536 - num13;
									}
									closestPoint.X.RawValue = num11 * a.X.RawValue + num13 * b.X.RawValue + 32768 >> 16;
									closestPoint.Y.RawValue = num11 * a.Y.RawValue + num13 * b.Y.RawValue + 32768 >> 16;
									closestPoint.Z.RawValue = num11 * a.Z.RawValue + num13 * b.Z.RawValue + 32768 >> 16;
									barycentricCoordinates.X.RawValue = num11;
									barycentricCoordinates.Y.RawValue = num13;
								}
								else
								{
									FP fP11 = default(FP);
									fP11.RawValue = fP8.RawValue + fP5.RawValue + fP10.RawValue;
									long num11;
									long num13;
									long num12;
									if (fP11.RawValue == 0L)
									{
										num11 = 21845L;
										num13 = 21845L;
										num12 = 21845L;
									}
									else
									{
										num13 = (fP5.RawValue << 16) / fP11.RawValue;
										num12 = (fP10.RawValue << 16) / fP11.RawValue;
										num11 = 65536 - num13 - num12;
									}
									closestPoint.X.RawValue = num11 * a.X.RawValue + num13 * b.X.RawValue + num12 * c.X.RawValue + 32768 >> 16;
									closestPoint.Y.RawValue = num11 * a.Y.RawValue + num13 * b.Y.RawValue + num12 * c.Y.RawValue + 32768 >> 16;
									closestPoint.Z.RawValue = num11 * a.Z.RawValue + num13 * b.Z.RawValue + num12 * c.Z.RawValue + 32768 >> 16;
									barycentricCoordinates.X.RawValue = num11;
									barycentricCoordinates.Y.RawValue = num13;
									barycentricCoordinates.Z.RawValue = num12;
								}
							}
						}
					}
				}
			}
			FP result = default(FP);
			result.RawValue = ((p.X.RawValue - closestPoint.X.RawValue) * (p.X.RawValue - closestPoint.X.RawValue) + 32768 >> 16) + ((p.Y.RawValue - closestPoint.Y.RawValue) * (p.Y.RawValue - closestPoint.Y.RawValue) + 32768 >> 16) + ((p.Z.RawValue - closestPoint.Z.RawValue) * (p.Z.RawValue - closestPoint.Z.RawValue) + 32768 >> 16);
			return result;
		}

		/// <summary>
		/// Checks if a point is inside the extents of an AABB centered at the origin (local space) and clamp it otherwise.
		/// </summary>
		/// <param name="point">Point in the local space of the AABB.</param>
		/// <param name="aabbExtents">Extents of the AABB.</param>
		/// <param name="clampedPoint">Clamped point inside the AABB. Equals to <paramref name="point" /> if it is already inside the AABB.</param>
		/// <returns><see langword="true" /> if the point is already inside the AABB.</returns>
		public static bool ClampPointToLocalAABB(FPVector3 point, FPVector3 aabbExtents, out FPVector3 clampedPoint)
		{
			bool result = true;
			if (point.X.RawValue < -aabbExtents.X.RawValue)
			{
				result = false;
				point.X.RawValue = -aabbExtents.X.RawValue;
			}
			else if (point.X.RawValue > aabbExtents.X.RawValue)
			{
				result = false;
				point.X.RawValue = aabbExtents.X.RawValue;
			}
			if (point.Y.RawValue < -aabbExtents.Y.RawValue)
			{
				result = false;
				point.Y.RawValue = -aabbExtents.Y.RawValue;
			}
			else if (point.Y.RawValue > aabbExtents.Y.RawValue)
			{
				result = false;
				point.Y.RawValue = aabbExtents.Y.RawValue;
			}
			if (point.Z.RawValue < -aabbExtents.Z.RawValue)
			{
				result = false;
				point.Z.RawValue = -aabbExtents.Z.RawValue;
			}
			else if (point.Z.RawValue > aabbExtents.Z.RawValue)
			{
				result = false;
				point.Z.RawValue = aabbExtents.Z.RawValue;
			}
			clampedPoint = point;
			return result;
		}

		/// <summary>
		/// Calculates the closest point on the line segment defined by two given points to a given point.
		/// </summary>
		/// <param name="point">The point to which the closest point on the segment is calculated.</param>
		/// <param name="a">The first point of the line segment.</param>
		/// <param name="b">The second point of the line segment.</param>
		/// <returns>The closest point on the line segment to the given point.</returns>
		public static FPVector3 ClosestPointInSegment(FPVector3 point, FPVector3 a, FPVector3 b)
		{
			FPVector3 fPVector = default(FPVector3);
			fPVector.X.RawValue = b.X.RawValue - a.X.RawValue;
			fPVector.Y.RawValue = b.Y.RawValue - a.Y.RawValue;
			fPVector.Z.RawValue = b.Z.RawValue - a.Z.RawValue;
			FP fP = default(FP);
			fP.RawValue = (fPVector.X.RawValue * fPVector.X.RawValue + 32768 >> 16) + (fPVector.Y.RawValue * fPVector.Y.RawValue + 32768 >> 16) + (fPVector.Z.RawValue * fPVector.Z.RawValue + 32768 >> 16);
			if (fP.RawValue <= 0)
			{
				return a;
			}
			FPVector3 fPVector2 = default(FPVector3);
			fPVector2.X.RawValue = point.X.RawValue - a.X.RawValue;
			fPVector2.Y.RawValue = point.Y.RawValue - a.Y.RawValue;
			fPVector2.Z.RawValue = point.Z.RawValue - a.Z.RawValue;
			FP fP2 = default(FP);
			fP2.RawValue = (fPVector2.X.RawValue * fPVector.X.RawValue + 32768 >> 16) + (fPVector2.Y.RawValue * fPVector.Y.RawValue + 32768 >> 16) + (fPVector2.Z.RawValue * fPVector.Z.RawValue + 32768 >> 16);
			fP2.RawValue = (fP2.RawValue << 16) / fP.RawValue;
			if (fP2.RawValue < 0)
			{
				return a;
			}
			if (fP2.RawValue > 65536)
			{
				return b;
			}
			a.X.RawValue += (b.X.RawValue - a.X.RawValue) * fP2.RawValue + 32768 >> 16;
			a.Y.RawValue += (b.Y.RawValue - a.Y.RawValue) * fP2.RawValue + 32768 >> 16;
			a.Z.RawValue += (b.Z.RawValue - a.Z.RawValue) * fP2.RawValue + 32768 >> 16;
			return a;
		}

		/// <summary>
		/// Computes the closes point in segment A to a segment B.
		/// </summary>
		/// <param name="segmentStartA">Start point of segment A.</param>
		/// <param name="segmentEndA">End point of segment A.</param>
		/// <param name="segmentStartB">Start point of segment A.</param>
		/// <param name="segmentEndB">End point of segment B.</param>
		/// <returns></returns>
		public static FPVector3 ClosestPointBetweenSegments(FPVector3 segmentStartA, FPVector3 segmentEndA, FPVector3 segmentStartB, FPVector3 segmentEndB)
		{
			FPVector3 fPVector = default(FPVector3);
			fPVector.X.RawValue = segmentEndA.X.RawValue - segmentStartA.X.RawValue;
			fPVector.Y.RawValue = segmentEndA.Y.RawValue - segmentStartA.Y.RawValue;
			fPVector.Z.RawValue = segmentEndA.Z.RawValue - segmentStartA.Z.RawValue;
			FPVector3 fPVector2 = default(FPVector3);
			fPVector2.X.RawValue = segmentEndB.X.RawValue - segmentStartB.X.RawValue;
			fPVector2.Y.RawValue = segmentEndB.Y.RawValue - segmentStartB.Y.RawValue;
			fPVector2.Z.RawValue = segmentEndB.Z.RawValue - segmentStartB.Z.RawValue;
			FPVector3 b = default(FPVector3);
			b.X.RawValue = segmentStartA.X.RawValue - segmentStartB.X.RawValue;
			b.Y.RawValue = segmentStartA.Y.RawValue - segmentStartB.Y.RawValue;
			b.Z.RawValue = segmentStartA.Z.RawValue - segmentStartB.Z.RawValue;
			FP fP = FPVector3.Dot(fPVector, fPVector);
			fP.RawValue = (fPVector.X.RawValue * fPVector.X.RawValue + 32768 >> 16) + (fPVector.Y.RawValue * fPVector.Y.RawValue + 32768 >> 16) + (fPVector.Z.RawValue * fPVector.Z.RawValue + 32768 >> 16);
			FP fP2 = FPVector3.Dot(fPVector, fPVector2);
			fP2.RawValue = (fPVector.X.RawValue * fPVector2.X.RawValue + 32768 >> 16) + (fPVector.Y.RawValue * fPVector2.Y.RawValue + 32768 >> 16) + (fPVector.Z.RawValue * fPVector2.Z.RawValue + 32768 >> 16);
			FP fP3 = FPVector3.Dot(fPVector2, fPVector2);
			fP3.RawValue = (fPVector2.X.RawValue * fPVector2.X.RawValue + 32768 >> 16) + (fPVector2.Y.RawValue * fPVector2.Y.RawValue + 32768 >> 16) + (fPVector2.Z.RawValue * fPVector2.Z.RawValue + 32768 >> 16);
			FP fP4 = FPVector3.Dot(fPVector, b);
			fP4.RawValue = (fPVector.X.RawValue * b.X.RawValue + 32768 >> 16) + (fPVector.Y.RawValue * b.Y.RawValue + 32768 >> 16) + (fPVector.Z.RawValue * b.Z.RawValue + 32768 >> 16);
			FP fP5 = FPVector3.Dot(fPVector2, b);
			fP5.RawValue = (fPVector2.X.RawValue * b.X.RawValue + 32768 >> 16) + (fPVector2.Y.RawValue * b.Y.RawValue + 32768 >> 16) + (fPVector2.Z.RawValue * b.Z.RawValue + 32768 >> 16);
			FP fP6 = fP * fP3 - fP2 * fP2;
			FP fP7 = fP6;
			FP fP8 = fP6;
			FP fP9;
			FP fP10;
			if (fP6 < FP.Epsilon)
			{
				fP9 = FP._0;
				fP7 = FP._1;
				fP10 = fP5;
				fP8 = fP3;
			}
			else
			{
				fP9 = fP2 * fP5 - fP3 * fP4;
				fP10 = fP * fP5 - fP2 * fP4;
				if (fP9 < FP._0)
				{
					fP9 = FP._0;
					fP10 = fP5;
					fP8 = fP3;
				}
				else if (fP9 > fP7)
				{
					fP9 = fP7;
					fP10 = fP5 + fP2;
					fP8 = fP3;
				}
			}
			if (fP10 < FP._0)
			{
				fP10 = FP._0;
				if (-fP4 < FP._0)
				{
					fP9 = FP._0;
				}
				else if (-fP4 > fP)
				{
					fP9 = fP7;
				}
				else
				{
					fP9 = -fP4;
					fP7 = fP;
				}
			}
			else if (fP10 > fP8)
			{
				fP10 = fP8;
				if (-fP4 + fP2 < FP._0)
				{
					fP9 = FP._0;
				}
				else if (-fP4 + fP2 > fP)
				{
					fP9 = fP7;
				}
				else
				{
					fP9 = -fP4 + fP2;
					fP7 = fP;
				}
			}
			FP fP11 = ((FPMath.Abs(fP9) < FP.Epsilon) ? FP._0 : (fP9 / fP7));
			FP fP12 = ((FPMath.Abs(fP10) < FP.Epsilon) ? FP._0 : (fP10 / fP8));
			FPVector3 fPVector3 = segmentStartA + fP11 * fPVector;
			FPVector3 b2 = segmentStartB + fP12 * fPVector2;
			FP fP13 = FPVector3.Distance(fPVector3, b2);
			if (fP13 < FP.Epsilon)
			{
				return fPVector3;
			}
			FP fP14 = FPVector3.Distance(segmentStartA, segmentStartB);
			FP fP15 = FPVector3.Distance(segmentStartA, segmentEndB);
			FP fP16 = FPVector3.Distance(segmentEndA, segmentStartB);
			FP fP17 = FPVector3.Distance(segmentEndA, segmentEndB);
			FP fP18 = fP13;
			FPVector3 result = fPVector3;
			if (fP14 < fP18)
			{
				fP18 = fP14;
				result = segmentStartA;
			}
			if (fP15 < fP18)
			{
				fP18 = fP15;
				result = segmentStartA;
			}
			if (fP16 < fP18)
			{
				fP18 = fP16;
				result = segmentEndA;
			}
			if (fP17 < fP18)
			{
				result = segmentEndA;
			}
			return result;
		}
	}
}

