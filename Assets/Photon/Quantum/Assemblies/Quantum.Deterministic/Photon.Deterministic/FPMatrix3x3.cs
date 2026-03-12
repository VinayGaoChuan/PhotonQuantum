using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Photon.Deterministic
{
	/// <summary>
	/// Represents 3x3 column major matrix.
	/// Each cell can be individually accessed as a field (M&lt;row&gt;&lt;column&gt;), with indexing
	/// indexing property[row, column] or with indexing property[index].
	/// </summary>
	/// \ingroup MathAPI
	[Serializable]
	[StructLayout(LayoutKind.Explicit)]
	public struct FPMatrix3x3
	{
		/// <summary>
		/// The size of the struct in-memory inside the Frame data-buffers or stack (when passed as value parameter).
		/// Not related to the snapshot payload this occupies, which is bit-packed and compressed.
		/// </summary>
		public const int SIZE = 72;

		/// <summary>First row, first column</summary>
		[FieldOffset(0)]
		public FP M00;

		/// <summary>Second row, first column</summary>
		[FieldOffset(8)]
		public FP M10;

		/// <summary>Third row, first column</summary>
		[FieldOffset(16)]
		public FP M20;

		/// <summary>First row, second column</summary>
		[FieldOffset(24)]
		public FP M01;

		/// <summary>Second row, second column</summary>
		[FieldOffset(32)]
		public FP M11;

		/// <summary>Third row, second column</summary>
		[FieldOffset(40)]
		public FP M21;

		/// <summary>First row, third column</summary>
		[FieldOffset(48)]
		public FP M02;

		/// <summary>Second row, third column</summary>
		[FieldOffset(56)]
		public FP M12;

		/// <summary>Third row, third column</summary>
		[FieldOffset(64)]
		public FP M22;

		/// <summary>
		/// Matrix with 0s in every cell.
		/// </summary>
		public static FPMatrix3x3 Zero => default(FPMatrix3x3);

		/// <summary>
		/// Matrix with 1s in the main diagonal and 0s in all other cells.
		/// </summary>
		public static FPMatrix3x3 Identity => new FPMatrix3x3
		{
			M00 = 
			{
				RawValue = 65536L
			},
			M11 = 
			{
				RawValue = 65536L
			},
			M22 = 
			{
				RawValue = 65536L
			}
		};

		/// <summary>
		/// Gets or sets cell M&lt;row&gt;&lt;column&gt;.
		/// </summary>
		/// <param name="row"></param>
		/// <param name="column"></param>
		/// <returns></returns>
		public FP this[int row, int column]
		{
			readonly get
			{
				return this[row + column * 3];
			}
			set
			{
				this[row + column * 3] = value;
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
					2 => M20, 
					3 => M01, 
					4 => M11, 
					5 => M21, 
					6 => M02, 
					7 => M12, 
					8 => M22, 
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
					M20 = value;
					break;
				case 3:
					M01 = value;
					break;
				case 4:
					M11 = value;
					break;
				case 5:
					M21 = value;
					break;
				case 6:
					M02 = value;
					break;
				case 7:
					M12 = value;
					break;
				case 8:
					M22 = value;
					break;
				default:
					throw new ArgumentOutOfRangeException();
				}
			}
		}

		/// <summary>
		/// Creates transposed matrix.
		/// </summary>
		public readonly FPMatrix3x3 Transposed => FromColumns(M00, M01, M02, M10, M11, M12, M20, M21, M22);

		/// <summary>
		/// Returns <see langword="true" /> if this matrix is equal to the <see cref="P:Photon.Deterministic.FPMatrix3x3.Identity" /> matrix
		/// </summary>
		public readonly bool IsIdentity
		{
			get
			{
				if (M00.RawValue == 65536 && M11.RawValue == 65536 && M22.RawValue == 65536)
				{
					return (M01.RawValue | M02.RawValue | M10.RawValue | M12.RawValue | M20.RawValue | M21.RawValue) == 0;
				}
				return false;
			}
		}

		/// <summary>
		/// Attempts to get a scale value from the matrix. 
		/// </summary>
		public readonly FPVector3 LossyScale
		{
			get
			{
				long x = (M00.RawValue * M00.RawValue + 32768 >> 16) + (M10.RawValue * M10.RawValue + 32768 >> 16) + (M20.RawValue * M20.RawValue + 32768 >> 16);
				long x2 = (M01.RawValue * M01.RawValue + 32768 >> 16) + (M11.RawValue * M11.RawValue + 32768 >> 16) + (M21.RawValue * M21.RawValue + 32768 >> 16);
				long x3 = (M02.RawValue * M02.RawValue + 32768 >> 16) + (M12.RawValue * M12.RawValue + 32768 >> 16) + (M22.RawValue * M22.RawValue + 32768 >> 16);
				return new FPVector3(FP.FromRaw(FPMath.SqrtRaw(x) * FPMath.SignInt(Determinant)), FP.FromRaw(FPMath.SqrtRaw(x2)), FP.FromRaw(FPMath.SqrtRaw(x3)));
			}
		}

		/// <summary>
		/// Creates inverted matrix. Matrix with determinant 0 can not be inverted and result with <see cref="P:Photon.Deterministic.FPMatrix3x3.Zero" />.
		/// </summary>
		public readonly FPMatrix3x3 Inverted
		{
			get
			{
				long num = (M11.RawValue * M22.RawValue + 32768 >> 16) - (M12.RawValue * M21.RawValue + 32768 >> 16);
				long num2 = (M10.RawValue * M22.RawValue + 32768 >> 16) - (M12.RawValue * M20.RawValue + 32768 >> 16);
				long num3 = (M10.RawValue * M21.RawValue + 32768 >> 16) - (M11.RawValue * M20.RawValue + 32768 >> 16);
				long num4 = (M00.RawValue * num + 32768 >> 16) - (M01.RawValue * num2 + 32768 >> 16) + (M02.RawValue * num3 + 32768 >> 16);
				if (num4 == 0L)
				{
					return Zero;
				}
				long num5 = 4294967296L / num4;
				FPMatrix3x3 result = default(FPMatrix3x3);
				result.M00.RawValue = num * num5 + 32768 >> 16;
				result.M01.RawValue = -(((M01.RawValue * M22.RawValue + 32768 >> 16) - (M02.RawValue * M21.RawValue + 32768 >> 16)) * num5 + 32768 >> 16);
				result.M02.RawValue = ((M01.RawValue * M12.RawValue + 32768 >> 16) - (M02.RawValue * M11.RawValue + 32768 >> 16)) * num5 + 32768 >> 16;
				result.M10.RawValue = -(num2 * num5 + 32768 >> 16);
				result.M11.RawValue = ((M00.RawValue * M22.RawValue + 32768 >> 16) - (M02.RawValue * M20.RawValue + 32768 >> 16)) * num5 + 32768 >> 16;
				result.M12.RawValue = -(((M00.RawValue * M12.RawValue + 32768 >> 16) - (M02.RawValue * M10.RawValue + 32768 >> 16)) * num5 + 32768 >> 16);
				result.M20.RawValue = num3 * num5 + 32768 >> 16;
				result.M21.RawValue = -(((M00.RawValue * M21.RawValue + 32768 >> 16) - (M01.RawValue * M20.RawValue + 32768 >> 16)) * num5 + 32768 >> 16);
				result.M22.RawValue = ((M00.RawValue * M11.RawValue + 32768 >> 16) - (M01.RawValue * M10.RawValue + 32768 >> 16)) * num5 + 32768 >> 16;
				return result;
			}
		}

		/// <summary>
		/// Calculates determinant of this matrix.
		/// </summary>
		public readonly FP Determinant => FP.FromRaw((M00.RawValue * ((M11.RawValue * M22.RawValue + 32768 >> 16) - (M12.RawValue * M21.RawValue + 32768 >> 16)) + 32768 >> 16) - (M01.RawValue * ((M10.RawValue * M22.RawValue + 32768 >> 16) - (M12.RawValue * M20.RawValue + 32768 >> 16)) + 32768 >> 16) + (M02.RawValue * ((M10.RawValue * M21.RawValue + 32768 >> 16) - (M11.RawValue * M20.RawValue + 32768 >> 16)) + 32768 >> 16));

		/// <summary>
		/// Attempts to get a rotation quaternion from this matrix.
		/// </summary>
		public readonly FPQuaternion Rotation
		{
			get
			{
				long num = M00.RawValue + M11.RawValue + M22.RawValue;
				FP w = default(FP);
				FP x = default(FP);
				FP y = default(FP);
				FP z = default(FP);
				if (num > 0)
				{
					long num2 = FPMath.SqrtRaw(num + 65536);
					w.RawValue = num2 >> 1;
					num2 = 2147483648u / num2;
					x.RawValue = (M21.RawValue - M12.RawValue) * num2 + 32768 >> 16;
					y.RawValue = (M02.RawValue - M20.RawValue) * num2 + 32768 >> 16;
					z.RawValue = (M10.RawValue - M01.RawValue) * num2 + 32768 >> 16;
				}
				else if ((M00 > M11) & (M00 > M22))
				{
					long num3 = FPMath.SqrtRaw(65536 + (M00.RawValue - M11.RawValue - M22.RawValue));
					x.RawValue = num3 >> 1;
					num3 = 2147483648u / num3;
					w.RawValue = (M21.RawValue - M12.RawValue) * num3 + 32768 >> 16;
					y.RawValue = (M01.RawValue + M10.RawValue) * num3 + 32768 >> 16;
					z.RawValue = (M02.RawValue + M20.RawValue) * num3 + 32768 >> 16;
				}
				else if (M11 > M22)
				{
					long num4 = FPMath.SqrtRaw(65536 + (M11.RawValue - M00.RawValue - M22.RawValue));
					y.RawValue = num4 >> 1;
					num4 = 2147483648u / num4;
					w.RawValue = (M02.RawValue - M20.RawValue) * num4 + 32768 >> 16;
					x.RawValue = (M01.RawValue + M10.RawValue) * num4 + 32768 >> 16;
					z.RawValue = (M12.RawValue + M21.RawValue) * num4 + 32768 >> 16;
				}
				else
				{
					long num5 = FPMath.SqrtRaw(65536 + (M22.RawValue - M00.RawValue - M11.RawValue));
					z.RawValue = num5 >> 1;
					num5 = 2147483648u / num5;
					w.RawValue = (M10.RawValue - M01.RawValue) * num5 + 32768 >> 16;
					x.RawValue = (M02.RawValue + M20.RawValue) * num5 + 32768 >> 16;
					y.RawValue = (M12.RawValue + M21.RawValue) * num5 + 32768 >> 16;
				}
				return new FPQuaternion(x, y, z, w);
			}
		}

		/// <summary>
		/// Create from rows - first three values set the first row, second three values - second row etc.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static FPMatrix3x3 FromRows(FP m00, FP m01, FP m02, FP m10, FP m11, FP m12, FP m20, FP m21, FP m22)
		{
			FPMatrix3x3 result = default(FPMatrix3x3);
			result.M00 = m00;
			result.M10 = m10;
			result.M20 = m20;
			result.M01 = m01;
			result.M11 = m11;
			result.M21 = m21;
			result.M02 = m02;
			result.M12 = m12;
			result.M22 = m22;
			return result;
		}

		/// <summary>
		/// Create from rows - the first vector set the first row, second vector - second row etc.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static FPMatrix3x3 FromRows(FPVector3 row0, FPVector3 row1, FPVector3 row2)
		{
			FPMatrix3x3 result = default(FPMatrix3x3);
			result.M00 = row0.X;
			result.M10 = row1.X;
			result.M20 = row2.X;
			result.M01 = row0.Y;
			result.M11 = row1.Y;
			result.M21 = row2.Y;
			result.M02 = row0.Z;
			result.M12 = row1.Z;
			result.M22 = row2.Z;
			return result;
		}

		/// <summary>
		/// Create from columns - first three values set the first column, second three values - second column etc.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static FPMatrix3x3 FromColumns(FP m00, FP m10, FP m20, FP m01, FP m11, FP m21, FP m02, FP m12, FP m22)
		{
			FPMatrix3x3 result = default(FPMatrix3x3);
			result.M00 = m00;
			result.M10 = m10;
			result.M20 = m20;
			result.M01 = m01;
			result.M11 = m11;
			result.M21 = m21;
			result.M02 = m02;
			result.M12 = m12;
			result.M22 = m22;
			return result;
		}

		/// <summary>
		/// Create from columns - the first vector set the first column, second vector - second column etc.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static FPMatrix3x3 FromColumns(FPVector3 column0, FPVector3 column1, FPVector3 column2)
		{
			FPMatrix3x3 result = default(FPMatrix3x3);
			result.M00 = column0.X;
			result.M10 = column0.Y;
			result.M20 = column0.Z;
			result.M01 = column1.X;
			result.M11 = column1.Y;
			result.M21 = column1.Z;
			result.M02 = column2.X;
			result.M12 = column2.Y;
			result.M22 = column2.Z;
			return result;
		}

		/// <summary>
		/// Creates a scaling matrix.
		/// </summary>
		/// <param name="scale"></param>
		/// <returns></returns>
		public static FPMatrix3x3 Scale(FPVector3 scale)
		{
			return FromColumns(scale.X, 0, 0, 0, scale.Y, 0, 0, 0, scale.Z);
		}

		/// <summary>
		/// Serializes an instance of FPMatrix3x3 using the given IDeterministicFrameSerializer.
		/// </summary>
		/// <param name="ptr">A pointer to the instance of FPMatrix3x3 to be serialized.</param>
		/// <param name="serializer">The IDeterministicFrameSerializer used for serialization.</param>
		public unsafe static void Serialize(void* ptr, IDeterministicFrameSerializer serializer)
		{
			FP.Serialize(&((FPMatrix3x3*)ptr)->M00, serializer);
			FP.Serialize(&((FPMatrix3x3*)ptr)->M10, serializer);
			FP.Serialize(&((FPMatrix3x3*)ptr)->M20, serializer);
			FP.Serialize(&((FPMatrix3x3*)ptr)->M01, serializer);
			FP.Serialize(&((FPMatrix3x3*)ptr)->M11, serializer);
			FP.Serialize(&((FPMatrix3x3*)ptr)->M21, serializer);
			FP.Serialize(&((FPMatrix3x3*)ptr)->M02, serializer);
			FP.Serialize(&((FPMatrix3x3*)ptr)->M12, serializer);
			FP.Serialize(&((FPMatrix3x3*)ptr)->M22, serializer);
		}

		/// <summary>
		/// Converts the FPMatrix3x3 to a string representation.
		/// The returned string has the format "(({0}, {1}, {2}), ({3}, {4}, {5}), ({6}, {7}, {8}))",
		/// where {0} to {8} are the formatted string representations of the matrix elements.
		/// </summary>
		/// <returns>A string representation of the FPMatrix3x3.</returns>
		public override readonly string ToString()
		{
			return string.Format(CultureInfo.InvariantCulture, "(({0}, {1}, {2}), ({3}, {4}, {5}), ({6}, {7}, {8}))", M00.AsFloat, M01.AsFloat, M02.AsFloat, M10.AsFloat, M11.AsFloat, M12.AsFloat, M20.AsFloat, M21.AsFloat, M22.AsFloat);
		}

		/// <summary>
		/// Calculates the hash code for the current FPMatrix3x3 instance.
		/// </summary>
		/// <returns>The calculated hash code.</returns>
		public override readonly int GetHashCode()
		{
			int num = 17;
			num = num * 31 + M00.GetHashCode();
			num = num * 31 + M10.GetHashCode();
			num = num * 31 + M20.GetHashCode();
			num = num * 31 + M01.GetHashCode();
			num = num * 31 + M11.GetHashCode();
			num = num * 31 + M21.GetHashCode();
			num = num * 31 + M02.GetHashCode();
			num = num * 31 + M12.GetHashCode();
			return num * 31 + M22.GetHashCode();
		}

		/// <summary>
		/// Adds two matrices.
		/// </summary>
		public static FPMatrix3x3 operator +(FPMatrix3x3 a, FPMatrix3x3 b)
		{
			a.M00.RawValue = a.M00.RawValue + b.M00.RawValue;
			a.M01.RawValue = a.M01.RawValue + b.M01.RawValue;
			a.M02.RawValue = a.M02.RawValue + b.M02.RawValue;
			a.M10.RawValue = a.M10.RawValue + b.M10.RawValue;
			a.M11.RawValue = a.M11.RawValue + b.M11.RawValue;
			a.M12.RawValue = a.M12.RawValue + b.M12.RawValue;
			a.M20.RawValue = a.M20.RawValue + b.M20.RawValue;
			a.M21.RawValue = a.M21.RawValue + b.M21.RawValue;
			a.M22.RawValue = a.M22.RawValue + b.M22.RawValue;
			return a;
		}

		/// <summary>
		/// Subtracts two matrices.
		/// </summary>
		public static FPMatrix3x3 operator -(FPMatrix3x3 a, FPMatrix3x3 b)
		{
			a.M00.RawValue = a.M00.RawValue - b.M00.RawValue;
			a.M01.RawValue = a.M01.RawValue - b.M01.RawValue;
			a.M02.RawValue = a.M02.RawValue - b.M02.RawValue;
			a.M10.RawValue = a.M10.RawValue - b.M10.RawValue;
			a.M11.RawValue = a.M11.RawValue - b.M11.RawValue;
			a.M12.RawValue = a.M12.RawValue - b.M12.RawValue;
			a.M20.RawValue = a.M20.RawValue - b.M20.RawValue;
			a.M21.RawValue = a.M21.RawValue - b.M21.RawValue;
			a.M22.RawValue = a.M22.RawValue - b.M22.RawValue;
			return a;
		}

		/// <summary>
		/// Multiplies two matrices.
		/// </summary>
		public static FPMatrix3x3 operator *(FPMatrix3x3 a, FPMatrix3x3 b)
		{
			FPMatrix3x3 result = default(FPMatrix3x3);
			result.M00.RawValue = (a.M00.RawValue * b.M00.RawValue + 32768 >> 16) + (a.M01.RawValue * b.M10.RawValue + 32768 >> 16) + (a.M02.RawValue * b.M20.RawValue + 32768 >> 16);
			result.M01.RawValue = (a.M00.RawValue * b.M01.RawValue + 32768 >> 16) + (a.M01.RawValue * b.M11.RawValue + 32768 >> 16) + (a.M02.RawValue * b.M21.RawValue + 32768 >> 16);
			result.M02.RawValue = (a.M00.RawValue * b.M02.RawValue + 32768 >> 16) + (a.M01.RawValue * b.M12.RawValue + 32768 >> 16) + (a.M02.RawValue * b.M22.RawValue + 32768 >> 16);
			result.M10.RawValue = (a.M10.RawValue * b.M00.RawValue + 32768 >> 16) + (a.M11.RawValue * b.M10.RawValue + 32768 >> 16) + (a.M12.RawValue * b.M20.RawValue + 32768 >> 16);
			result.M11.RawValue = (a.M10.RawValue * b.M01.RawValue + 32768 >> 16) + (a.M11.RawValue * b.M11.RawValue + 32768 >> 16) + (a.M12.RawValue * b.M21.RawValue + 32768 >> 16);
			result.M12.RawValue = (a.M10.RawValue * b.M02.RawValue + 32768 >> 16) + (a.M11.RawValue * b.M12.RawValue + 32768 >> 16) + (a.M12.RawValue * b.M22.RawValue + 32768 >> 16);
			result.M20.RawValue = (a.M20.RawValue * b.M00.RawValue + 32768 >> 16) + (a.M21.RawValue * b.M10.RawValue + 32768 >> 16) + (a.M22.RawValue * b.M20.RawValue + 32768 >> 16);
			result.M21.RawValue = (a.M20.RawValue * b.M01.RawValue + 32768 >> 16) + (a.M21.RawValue * b.M11.RawValue + 32768 >> 16) + (a.M22.RawValue * b.M21.RawValue + 32768 >> 16);
			result.M22.RawValue = (a.M20.RawValue * b.M02.RawValue + 32768 >> 16) + (a.M21.RawValue * b.M12.RawValue + 32768 >> 16) + (a.M22.RawValue * b.M22.RawValue + 32768 >> 16);
			return result;
		}

		/// <summary>
		/// Multiplies a vector by a matrix.
		/// </summary>
		public static FPVector3 operator *(FPMatrix3x3 m, FPVector3 vector)
		{
			FPVector3 result = default(FPVector3);
			result.X.RawValue = (m.M00.RawValue * vector.X.RawValue + 32768 >> 16) + (m.M01.RawValue * vector.Y.RawValue + 32768 >> 16) + (m.M02.RawValue * vector.Z.RawValue + 32768 >> 16);
			result.Y.RawValue = (m.M10.RawValue * vector.X.RawValue + 32768 >> 16) + (m.M11.RawValue * vector.Y.RawValue + 32768 >> 16) + (m.M12.RawValue * vector.Z.RawValue + 32768 >> 16);
			result.Z.RawValue = (m.M20.RawValue * vector.X.RawValue + 32768 >> 16) + (m.M21.RawValue * vector.Y.RawValue + 32768 >> 16) + (m.M22.RawValue * vector.Z.RawValue + 32768 >> 16);
			return result;
		}

		/// <summary>
		/// Multiplies a matrix by a factor.
		/// </summary>
		public static FPMatrix3x3 operator *(FP a, FPMatrix3x3 m)
		{
			m.M00.RawValue = a.RawValue * m.M00.RawValue + 32768 >> 16;
			m.M01.RawValue = a.RawValue * m.M01.RawValue + 32768 >> 16;
			m.M02.RawValue = a.RawValue * m.M02.RawValue + 32768 >> 16;
			m.M10.RawValue = a.RawValue * m.M10.RawValue + 32768 >> 16;
			m.M11.RawValue = a.RawValue * m.M11.RawValue + 32768 >> 16;
			m.M12.RawValue = a.RawValue * m.M12.RawValue + 32768 >> 16;
			m.M20.RawValue = a.RawValue * m.M20.RawValue + 32768 >> 16;
			m.M21.RawValue = a.RawValue * m.M21.RawValue + 32768 >> 16;
			m.M22.RawValue = a.RawValue * m.M22.RawValue + 32768 >> 16;
			return m;
		}

		/// <summary>
		/// Creates a rotation matrix. Rotation is expected to be normalized.
		/// </summary>
		public static FPMatrix3x3 Rotate(FPQuaternion q)
		{
			long num = q.X.RawValue * 2;
			long num2 = q.Y.RawValue * 2;
			long num3 = q.Z.RawValue * 2;
			long num4 = q.X.RawValue * num + 32768 >> 16;
			long num5 = q.Y.RawValue * num2 + 32768 >> 16;
			long num6 = q.Z.RawValue * num3 + 32768 >> 16;
			long num7 = q.X.RawValue * num2 + 32768 >> 16;
			long num8 = q.X.RawValue * num3 + 32768 >> 16;
			long num9 = q.Y.RawValue * num3 + 32768 >> 16;
			long num10 = q.W.RawValue * num + 32768 >> 16;
			long num11 = q.W.RawValue * num2 + 32768 >> 16;
			long num12 = q.W.RawValue * num3 + 32768 >> 16;
			FPMatrix3x3 result = default(FPMatrix3x3);
			result.M00.RawValue = 65536 - (num5 + num6);
			result.M10.RawValue = num7 + num12;
			result.M20.RawValue = num8 - num11;
			result.M01.RawValue = num7 - num12;
			result.M11.RawValue = 65536 - (num4 + num6);
			result.M21.RawValue = num9 + num10;
			result.M02.RawValue = num8 + num11;
			result.M12.RawValue = num9 - num10;
			result.M22.RawValue = 65536 - (num4 + num5);
			return result;
		}

		/// <summary>
		/// Creates a rotation and scaling matrix.
		/// Rotation is expected to be normalized.
		/// </summary>
		public static FPMatrix3x3 RotateScale(FPQuaternion q, FPVector3 s)
		{
			FPMatrix3x3 result = Rotate(q);
			result.M00.RawValue = result.M00.RawValue * s.X.RawValue + 32768 >> 16;
			result.M10.RawValue = result.M10.RawValue * s.X.RawValue + 32768 >> 16;
			result.M20.RawValue = result.M20.RawValue * s.X.RawValue + 32768 >> 16;
			result.M01.RawValue = result.M01.RawValue * s.Y.RawValue + 32768 >> 16;
			result.M11.RawValue = result.M11.RawValue * s.Y.RawValue + 32768 >> 16;
			result.M21.RawValue = result.M21.RawValue * s.Y.RawValue + 32768 >> 16;
			result.M02.RawValue = result.M02.RawValue * s.Z.RawValue + 32768 >> 16;
			result.M12.RawValue = result.M12.RawValue * s.Z.RawValue + 32768 >> 16;
			result.M22.RawValue = result.M22.RawValue * s.Z.RawValue + 32768 >> 16;
			return result;
		}

		/// <summary>
		/// Creates an inverse rotation and scaling matrix. This is significantly faster than inverting a RotateScale matrix. 
		/// Rotation is expected to be normalized.
		/// </summary>
		public static FPMatrix3x3 InverseRotateScale(FPQuaternion q, FPVector3 s)
		{
			FPMatrix3x3 result = Rotate(q.Conjugated);
			result.M00.RawValue = result.M00.RawValue * s.X.RawValue + 32768 >> 16;
			result.M01.RawValue = result.M01.RawValue * s.X.RawValue + 32768 >> 16;
			result.M02.RawValue = result.M02.RawValue * s.X.RawValue + 32768 >> 16;
			result.M10.RawValue = result.M10.RawValue * s.Y.RawValue + 32768 >> 16;
			result.M11.RawValue = result.M11.RawValue * s.Y.RawValue + 32768 >> 16;
			result.M12.RawValue = result.M12.RawValue * s.Y.RawValue + 32768 >> 16;
			result.M20.RawValue = result.M20.RawValue * s.Z.RawValue + 32768 >> 16;
			result.M21.RawValue = result.M21.RawValue * s.Z.RawValue + 32768 >> 16;
			result.M22.RawValue = result.M22.RawValue * s.Z.RawValue + 32768 >> 16;
			return result;
		}
	}
}

