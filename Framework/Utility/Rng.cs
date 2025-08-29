using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Foster.Framework;

/// <summary>
/// Lightweight random number generator
/// </summary>
public struct Rng
{
	public ulong Seed;

	public Rng() { Seed = 0; }
	public Rng(int seed) { Seed = (ulong)seed; }
	public Rng(ulong seed) { Seed = seed; }

	public ulong U64()
	{
		Seed += 0x9e3779b97f4a7c15ul;
		ulong n = Seed;
		n = (n ^ (n >> 30)) * 0xbf58476d1ce4e5bul;
		n = (n ^ (n >> 27)) * 0x94d049bb133111ebul;
		return n ^ (n >> 31);
	}
	public ulong U64(ulong max) => max != 0 ? U64() % max : 0;
	public ulong U64(ulong min, ulong max) => min + U64(max - min);

	public uint U32() => (uint)U64();
	public uint U32(uint max) => max != 0 ? U32() % max : 0;
	public uint U32(uint min, uint max) => min + U32(max - min);

	public ushort U16() => (ushort)U64();
	public ushort U16(ushort max) => (ushort)(max != 0 ? U16() % max : 0);
	public ushort U16(ushort min, ushort max) => (ushort)(min + U16((ushort)(max - min)));

	public byte U8() => (byte)U64();
	public byte U8(byte max) => (byte)(max != 0 ? U8() % max : 0);
	public byte U8(byte min, byte max) => (byte)(min + U8((byte)(max - min)));

	public long Long() => (long)U64();
	public long Long(long max) => max != 0 ? Math.Abs(Long()) % max : 0;
	public long Long(long min, long max) => min + Long(max - min);

	public int Int() => (int)U64();
	public int Int(int max) => max != 0 ? Math.Abs(Int()) % max : 0;
	public int Int(int min, int max) => min + Int(max - min);

	public short Short() => (short)U64();
	public short Short(short max) => (short)(max != 0 ? Math.Abs(Short()) % max : 0);
	public short Short(short min, short max) => (short)(min + Short((short)(max - min)));

	[Obsolete("Use SByte or U8")] public sbyte Byte() => (sbyte)U64();
	[Obsolete("Use SByte or U8")] public sbyte Byte(sbyte max) => (sbyte)(max != 0 ? Math.Abs(SByte()) % max : 0);
	[Obsolete("Use SByte or U8")] public sbyte Byte(sbyte min, sbyte max) => (sbyte)(min + SByte((sbyte)(max - min)));

	public sbyte SByte() => (sbyte)U64();
	public sbyte SByte(sbyte max) => (sbyte)(max != 0 ? Math.Abs(SByte()) % max : 0);
	public sbyte SByte(sbyte min, sbyte max) => (sbyte)(min + SByte((sbyte)(max - min)));

	public float Float()
	{
		uint bits = (uint)(0x3f800000ul | (U64() >> 40));
		float val;
		unsafe { Unsafe.WriteUnaligned(&val, bits); }
		return val - (1.0f - float.Epsilon / 2.0f);
	}
	public float Float(float max) => Float() * max;
	public float Float(float min, float max) => min + Float(max - min);

	public double Double()
	{
		ulong bits = 0x3ff0000000000000ul | (U64() >> 11);
		double val;
		unsafe { Unsafe.WriteUnaligned(&val, bits); }
		return val - (1.0 - double.Epsilon / 2.0);
	}
	public double Double(double max) => Double() * max;
	public double Double(double min, double max) => min + Double(max - min);

	public bool Boolean() => (U64() & 1) == 1;
	public int Sign() => Boolean() ? 1 : -1;
	public bool Chance(float percent) => Float() <= percent;

	public T Choose<T>(params ReadOnlySpan<T> choices) => choices[Int(choices.Length)];
	public T Choose<T>(params T[] choices) => choices[Int(choices.Length)];
	public T Choose<T>(params IList<T> choices) => choices[Int(choices.Count)];

	public Point2 Shake() => new(Choose(-1, 0, 1), Choose(-1, 0, 1));
	public float Angle() => Float(Calc.TAU);

	public System.Numerics.Vector2 PointInside(in Rect rect)
		=> rect.On(Float(1), Float(1));

	public void Shuffle<T>(Span<T> span)
	{
		int n = span.Length;
		while (n > 1) {
			n--;
			int k = Int(n + 1);
			(span[k], span[n]) = (span[n], span[k]);
		}
	}
	public void Shuffle<T>(T[] array) => Shuffle(array.AsSpan());
	public void Shuffle<T>(List<T> list) => Shuffle(CollectionsMarshal.AsSpan(list));

	public static Rng Randomized() => new((ulong)DateTime.Now.Ticks);
}
