using System;
using System.Runtime.CompilerServices;

namespace Foster.Framework;

/// <summary>
/// A Struct for managing Masks
/// </summary>
public struct Mask
{
	public const ulong All = 0xFFFFFFFFFFFFFFFF;
	public const ulong None = 0;
	public const ulong Default = (1 << 0);

	public ulong Value;

	public Mask(ulong value)
	{
		Value = value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Add(Mask mask)
	{
		Value |= mask.Value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Remove(Mask mask)
	{
		Value &= ~mask.Value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Has(Mask mask)
	{
		return (Value & mask.Value) > 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Mask Make(int index)
	{
		if (index < 0 || index > 63)
			throw new ArgumentOutOfRangeException(nameof(index), "Index must be between 0 and 63");

		return new Mask(((ulong)1 << index));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Mask operator |(Mask a, Mask b) => new Mask(a.Value | b.Value);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Mask operator &(Mask a, Mask b) => new Mask(a.Value & b.Value);

	public static implicit operator ulong(Mask mask) => mask.Value;
	public static implicit operator Mask(ulong val) => new Mask(val);
}
