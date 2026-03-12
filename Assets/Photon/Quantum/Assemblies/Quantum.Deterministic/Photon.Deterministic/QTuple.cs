using System.Runtime.InteropServices;

namespace Photon.Deterministic
{
	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct QTuple
	{
		public static QTuple<T0, T1> Create<T0, T1>(T0 item0, T1 item1)
		{
			return new QTuple<T0, T1>(item0, item1);
		}

		public static QTuple<T0, T1, T2> Create<T0, T1, T2>(T0 item0, T1 item1, T2 item2)
		{
			return new QTuple<T0, T1, T2>(item0, item1, item2);
		}

		public static QTuple<T0, T1, T2, T3> Create<T0, T1, T2, T3>(T0 item0, T1 item1, T2 item2, T3 item3)
		{
			return new QTuple<T0, T1, T2, T3>(item0, item1, item2, item3);
		}
	}
	public struct QTuple<T0, T1>
	{
		public readonly T0 Item0;

		public readonly T1 Item1;

		public QTuple(T0 item0, T1 item1)
		{
			Item0 = item0;
			Item1 = item1;
		}

		public void Deconstruct(out T0 item0, out T1 item1)
		{
			item0 = Item0;
			item1 = Item1;
		}
	}
	public struct QTuple<T0, T1, T2>
	{
		public readonly T0 Item0;

		public readonly T1 Item1;

		public readonly T2 Item2;

		public QTuple(T0 item0, T1 item1, T2 item2)
		{
			Item0 = item0;
			Item1 = item1;
			Item2 = item2;
		}

		public void Deconstruct(out T0 item0, out T1 item1, out T2 item2)
		{
			item0 = Item0;
			item1 = Item1;
			item2 = Item2;
		}
	}
	public struct QTuple<T0, T1, T2, T3>
	{
		public readonly T0 Item0;

		public readonly T1 Item1;

		public readonly T2 Item2;

		public readonly T3 Item3;

		public QTuple(T0 item0, T1 item1, T2 item2, T3 item3)
		{
			Item0 = item0;
			Item1 = item1;
			Item2 = item2;
			Item3 = item3;
		}

		public void Deconstruct(out T0 item0, out T1 item1, out T2 item2, out T3 item3)
		{
			item0 = Item0;
			item1 = Item1;
			item2 = Item2;
			item3 = Item3;
		}
	}
}

