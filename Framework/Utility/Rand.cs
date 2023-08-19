using System.Runtime.CompilerServices;

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

	public sbyte Byte() => (sbyte)U64();
	public sbyte Byte(sbyte max) => (sbyte)(max != 0 ? Math.Abs(Byte()) % max : 0);
	public sbyte Byte(sbyte min, sbyte max) => (sbyte)(min + Byte((sbyte)(max - min)));

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
		ulong bits = (uint)(0x3ff0000000000000ul | (U64() >> 11));
		double val;
		unsafe { Unsafe.WriteUnaligned(&val, bits); }
		return val - (1.0 - double.Epsilon / 2.0);
	}
	public double Double(double max) => Double() * max;
	public double Double(double min, double max) => min + Double(max - min);

	public bool Boolean() => (U64() & 1) == 1;
	public int Sign() => Boolean() ? 1 : -1;
	public bool Chance(float percent) => Float() <= percent;

	public T Choose<T>(T a, T b)
		=> U8(2) switch
		{
			1 => b,
			_ => a,
		};

	public T Choose<T>(T a, T b, T c)
		=> U8(3) switch
		{
			1 => b,
			2 => c,
			_ => a,
		};

	public T Choose<T>(T a, T b, T c, T d)
		=> U8(4) switch
		{
			1 => b,
			2 => c,
			3 => d,
			_ => a,
		};

	public T Choose<T>(T a, T b, T c, T d, T e)
		=> U8(5) switch
		{
			1 => b,
			2 => c,
			3 => d,
			4 => e,
			_ => a,
		};

	public T Choose<T>(T a, T b, T c, T d, T e, T f)
		=> U8(6) switch
		{
			1 => b,
			2 => c,
			3 => d,
			4 => e,
			5 => f,
			_ => a,
		};
}