using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace Photon.Deterministic
{
	/// <summary>
	/// Represents 2x2 column major matrix, which can be used for 2D scaling and rotation.
	/// Each cell can be individually accessed as a field (M&lt;row&gt;&lt;column&gt;).
	/// </summary>
	/// \ingroup MathAPI
	[Serializable]
	[StructLayout(LayoutKind.Explicit)]
	public struct FPMatrix2x2
	{
		/// <summary>
		/// The size of the struct in-memory inside the Frame data-buffers or stack (when passed as value parameter).
		/// Not related to the snapshot payload this occupies, which is bit-packed and compressed.
		/// </summary>
		public const int SIZE = 32;

		/// <summary>
		/// The value of the element at the first row and first column of a 2x2 matrix.
		/// </summary>
		[FieldOffset(0)]
		public FP M00;

		/// <summary>
		/// The value of the element at the second row and second column of a 2x2 matrix.
		/// </summary>
		[FieldOffset(8)]
		public FP M10;

		/// <summary>
		/// The value of the element at the first row and second column of a 2x2 matrix.
		/// </summary>
		[FieldOffset(16)]
		public FP M01;

		/// <summary>
		/// The value of the element at the second row and first column of a 2x2 matrix.
		/// </summary>
		[FieldOffset(24)]
		public FP M11;

		/// <summary>
		/// Matrix with 0s in every cell.
		/// </summary>
		public static FPMatrix2x2 Zero => default(FPMatrix2x2);

		/// <summary>
		/// Matrix with 1s in the main diagonal and 0s in all other cells.
		/// </summary>
		public static FPMatrix2x2 Identity => new FPMatrix2x2
		{
			M00 = 
			{
				RawValue = 65536L
			},
			M11 = 
			{
				RawValue = 65536L
			}
		};

		/// <summary>
		/// Returns <see langword="true" /> if this matrix is equal to the <see cref="P:Photon.Deterministic.FPMatrix2x2.Identity" /> matrix
		/// </summary>
		public readonly bool IsIdentity
		{
			get
			{
				if (M00.RawValue == 65536 && M11.RawValue == 65536)
				{
					return (M01.RawValue | M10.RawValue) == 0;
				}
				return false;
			}
		}

		/// <summary>
		/// Gets or sets cell M&lt;index%4&gt;&lt;index/4&gt;
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public FP this[int index]
		{
			readonly get
			{
				return index switch
				{
					0 => M00, 
					1 => M10, 
					2 => M01, 
					3 => M11, 
					_ => throw new ArgumentOutOfRangeException(), 
				};
			}
			set
			{
				switch (index)
				{
				case 0:
					M00 = value;
					break;
				case 1:
					M10 = value;
					break;
				case 2:
					M01 = value;
					break;
				case 3:
					M11 = value;
					break;
				default:
					throw new ArgumentOutOfRangeException();
				}
			}
		}

		/// <summary>
		/// Attempts to get a scale value from the matrix. 
		/// </summary>
		public readonly FPVector2 LossyScale
		{
			get
			{
				long x = (M00.RawValue * M00.RawValue + 32768 >> 16) + (M10.RawValue * M10.RawValue + 32768 >> 16);
				long x2 = (M01.RawValue * M01.RawValue + 32768 >> 16) + (M11.RawValue * M11.RawValue + 32768 >> 16);
				return new FPVector2(FP.FromRaw(FPMath.SqrtRaw(x) * FPMath.SignInt(Determinant)), FP.FromRaw(FPMath.SqrtRaw(x2)));
			}
		}

		/// <summary>
		/// Creates inverted matrix. Matrix with determinant 0 can not be inverted and result with <see cref="P:Photon.Deterministic.FPMatrix2x2.Zero" />.
		/// </summary>
		public readonly FPMatrix2x2 Inverted
		{
			get
			{
				long num = (M00.RawValue * M11.RawValue + 32768 >> 16) - (M10.RawValue * M01.RawValue + 32768 >> 16);
				if (num == 0L)
				{
					return Zero;
				}
				long num2 = 4294967296L / num;
				FPMatrix2x2 result = default(FPMatrix2x2);
				result.M00.RawValue = M11.RawValue * num2 + 32768 >> 16;
				result.M01.RawValue = -(M01.RawValue * num2 + 32768 >> 16);
				result.M10.RawValue = -(M10.RawValue * num2 + 32768 >> 16);
				result.M11.RawValue = M00.RawValue * num2 + 32768 >> 16;
				return result;
			}
		}

		/// <summary>
		/// Calculates determinant of this matrix.
		/// </summary>
		public readonly FP Determinant => FP.FromRaw((M00.RawValue * M11.RawValue + 32768 >> 16) - (M10.RawValue * M01.RawValue + 32768 >> 16));

		/// <summary>
		/// Create from columns - first two values set the first row, second two values - second row.
		/// </summary>
		public static FPMatrix2x2 FromRows(FP m00, FP m01, FP m10, FP m11)
		{
			FPMatrix2x2 result = default(FPMatrix2x2);
			result.M00 = m00;
			result.M10 = m10;
			result.M01 = m01;
			result.M11 = m11;
			return result;
		}

		/// <summary>
		/// Create from rows - first vector set the first row, second vector set the second row.
		/// </summary>
		public static FPMatrix2x2 FromRows(FPVector2 row0, FPVector2 row1)
		{
			FPMatrix2x2 result = default(FPMatrix2x2);
			result.M00 = row0.X;
			result.M10 = row1.X;
			result.M01 = row0.Y;
			result.M11 = row1.Y;
			return result;
		}

		/// <summary>
		/// Create from columns - first two values set the first colunn, second two values - second column.
		/// </summary>
		public static FPMatrix2x2 FromColumns(FP m00, FP m10, FP m01, FP m11)
		{
			FPMatrix2x2 result = default(FPMatrix2x2);
			result.M00 = m00;
			result.M10 = m10;
			result.M01 = m01;
			result.M11 = m11;
			return result;
		}

		/// <summary>
		/// Create from columns - first vector set the first column, second vector set second column.
		/// </summary>
		public static FPMatrix2x2 FromColumns(FPVector2 column0, FPVector2 column1)
		{
			FPMatrix2x2 result = default(FPMatrix2x2);
			result.M00 = column0.X;
			result.M10 = column0.Y;
			result.M01 = column1.X;
			result.M11 = column1.Y;
			return result;
		}

		/// <summary>
		/// Creates a rotation matrix.
		/// </summary>
		/// <param name="rotation">Rotation in radians.</param>
		public static FPMatrix2x2 Rotate(FP rotation)
		{
			FPMatrix2x2 result = default(FPMatrix2x2);
			FPMath.SinCos(rotation, out result.M01, out result.M00);
			result.M10 = -result.M01;
			result.M11 = result.M00;
			return result;
		}

		/// <summary>
		/// Creates a scaling matrix.
		/// </summary>
		public static FPMatrix2x2 Scale(FPVector2 scale)
		{
			return FromColumns(scale.X, 0, 0, scale.Y);
		}

		/// <summary>
		/// Transforms a direction by this matrix.
		/// </summary>
		public readonly FPVector2 MultiplyVector(FPVector2 v)
		{
			FPVector2 result = default(FPVector2);
			result.X.RawValue = (M00.RawValue * v.X.RawValue + 32768 >> 16) + (M01.RawValue * v.Y.RawValue + 32768 >> 16);
			result.Y.RawValue = (M10.RawValue * v.X.RawValue + 32768 >> 16) + (M11.RawValue * v.Y.RawValue + 32768 >> 16);
			return result;
		}

		/// <summary>
		/// Serializes the FPMatrix2x2 instance into a byte stream using the specified serializer.
		/// </summary>
		/// <param name="ptr">A pointer to the FPMatrix2x2 instance.</param>
		/// <param name="serializer">The serializer used to write the data.</param>
		public unsafe static void Serialize(void* ptr, IDeterministicFrameSerializer serializer)
		{
			FP.Serialize(&((FPMatrix2x2*)ptr)->M00, serializer);
			FP.Serialize(&((FPMatrix2x2*)ptr)->M10, serializer);
			FP.Serialize(&((FPMatrix2x2*)ptr)->M01, serializer);
			FP.Serialize(&((FPMatrix2x2*)ptr)->M11, serializer);
		}

		/// <summary>
		/// Returns a string representation of the current FPMatrix2x2 object.
		/// </summary>
		/// <returns>
		/// A string that represents the current FPMatrix2x2 object. The string is formatted as "(({0}, {1}), ({2}, {3}))" where {0} represents the value of M00, {1} represents the value of M01, {2} represents the value of M10, and {3} represents the value of M11. The values are formatted using the InvariantCulture.
		/// </returns>
		public override readonly string ToString()
		{
			return string.Format(CultureInfo.InvariantCulture, "(({0}, {1}), ({2}, {3}))", M00.AsFloat, M01.AsFloat, M10.AsFloat, M11.AsFloat);
		}

		/// <summary>
		/// Calculates the hash code for the FPMatrix2x2 object.
		/// </summary>
		/// <returns>The hash code value for the current instance.</returns>
		public override readonly int GetHashCode()
		{
			int num = 17;
			num = num * 31 + M00.GetHashCode();
			num = num * 31 + M10.GetHashCode();
			num = num * 31 + M01.GetHashCode();
			return num * 31 + M11.GetHashCode();
		}

		/// <summary>
		/// Adds two matrices.
		/// </summary>
		public static FPMatrix2x2 operator +(FPMatrix2x2 a, FPMatrix2x2 b)
		{
			a.M00.RawValue = a.M00.RawValue + b.M00.RawValue;
			a.M01.RawValue = a.M01.RawValue + b.M01.RawValue;
			a.M10.RawValue = a.M10.RawValue + b.M10.RawValue;
			a.M11.RawValue = a.M11.RawValue + b.M11.RawValue;
			return a;
		}

		/// <summary>
		/// Subtracts two matrices.
		/// </summary>
		public static FPMatrix2x2 operator -(FPMatrix2x2 a, FPMatrix2x2 b)
		{
			a.M00.RawValue = a.M00.RawValue - b.M00.RawValue;
			a.M01.RawValue = a.M01.RawValue - b.M01.RawValue;
			a.M10.RawValue = a.M10.RawValue - b.M10.RawValue;
			a.M11.RawValue = a.M11.RawValue - b.M11.RawValue;
			return a;
		}

		/// <summary>
		/// Multiplies two matrices.
		/// </summary>
		public static FPMatrix2x2 operator *(FPMatrix2x2 a, FPMatrix2x2 b)
		{
			FPMatrix2x2 result = default(FPMatrix2x2);
			result.M00.RawValue = (a.M00.RawValue * b.M00.RawValue + 32768 >> 16) + (a.M01.RawValue * b.M10.RawValue + 32768 >> 16);
			result.M01.RawValue = (a.M00.RawValue * b.M01.RawValue + 32768 >> 16) + (a.M01.RawValue * b.M11.RawValue + 32768 >> 16);
			result.M10.RawValue = (a.M10.RawValue * b.M00.RawValue + 32768 >> 16) + (a.M11.RawValue * b.M10.RawValue + 32768 >> 16);
			result.M11.RawValue = (a.M10.RawValue * b.M01.RawValue + 32768 >> 16) + (a.M11.RawValue * b.M11.RawValue + 32768 >> 16);
			return result;
		}

		/// <summary>
		/// Multiplies a vector by a matrix.
		/// </summary>
		public static FPVector2 operator *(FPMatrix2x2 m, FPVector2 vector)
		{
			FPVector2 result = default(FPVector2);
			result.X.RawValue = (m.M00.RawValue * vector.X.RawValue + 32768 >> 16) + (m.M01.RawValue * vector.Y.RawValue + 32768 >> 16);
			result.Y.RawValue = (m.M10.RawValue * vector.X.RawValue + 32768 >> 16) + (m.M11.RawValue * vector.Y.RawValue + 32768 >> 16);
			return result;
		}

		/// <summary>
		/// Multiplies a matrix by a factor.
		/// </summary>
		public static FPMatrix2x2 operator *(FP a, FPMatrix2x2 m)
		{
			m.M00.RawValue = a.RawValue * m.M00.RawValue + 32768 >> 16;
			m.M01.RawValue = a.RawValue * m.M01.RawValue + 32768 >> 16;
			m.M10.RawValue = a.RawValue * m.M10.RawValue + 32768 >> 16;
			m.M11.RawValue = a.RawValue * m.M11.RawValue + 32768 >> 16;
			return m;
		}
	}
}

