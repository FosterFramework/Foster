using System.Runtime.CompilerServices;

namespace Foster.Framework;

/// <summary>
/// <see cref="Enum"/> Extension Methods
/// </summary>
public static class EnumExt
{
	extension<T>(T flags) where T : unmanaged, Enum
	{
		/// <summary>
		/// Bitwise check if <paramref name="flags"/> has set any of the set bits in <paramref name="check"/>
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe bool Has(T check)
			=> sizeof(T) switch
			{
				1 => (Unsafe.BitCast<T, byte>(flags) & Unsafe.BitCast<T, byte>(check)) > 0,
				2 => (Unsafe.BitCast<T, ushort>(flags) & Unsafe.BitCast<T, ushort>(check)) > 0,
				4 => (Unsafe.BitCast<T, uint>(flags) & Unsafe.BitCast<T, uint>(check)) > 0,
				8 => (Unsafe.BitCast<T, ulong>(flags) & Unsafe.BitCast<T, ulong>(check)) > 0,
				_ => throw new Exception("Size does not match a known Enum backing type."),
			};

		/// <summary>
		/// Bitwise check if <paramref name="flags"/> has set all of the set bits in <paramref name="check"/>
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe bool HasAll(T check)
			=> sizeof(T) switch
			{
				1 => (Unsafe.BitCast<T, byte>(flags) & Unsafe.BitCast<T, byte>(check)) == Unsafe.BitCast<T, byte>(check),
				2 => (Unsafe.BitCast<T, ushort>(flags) & Unsafe.BitCast<T, ushort>(check)) == Unsafe.BitCast<T, ushort>(check),
				4 => (Unsafe.BitCast<T, uint>(flags) & Unsafe.BitCast<T, uint>(check)) == Unsafe.BitCast<T, uint>(check),
				8 => (Unsafe.BitCast<T, ulong>(flags) & Unsafe.BitCast<T, ulong>(check)) == Unsafe.BitCast<T, ulong>(check),
				_ => throw new Exception("Size does not match a known Enum backing type."),
			};

		/// <summary>
		/// Return <paramref name="flags"/> after setting all the set bits in <paramref name="with"/>
		/// </summary>
		public unsafe T With(T with)
			=> sizeof(T) switch
			{
				1 => Unsafe.BitCast<byte, T>((byte)(Unsafe.BitCast<T, byte>(flags) | Unsafe.BitCast<T, byte>(with))),
				2 => Unsafe.BitCast<ushort, T>((ushort)(Unsafe.BitCast<T, ushort>(flags) | Unsafe.BitCast<T, ushort>(with))),
				4 => Unsafe.BitCast<uint, T>(Unsafe.BitCast<T, uint>(flags) | Unsafe.BitCast<T, uint>(with)),
				8 => Unsafe.BitCast<ulong, T>(Unsafe.BitCast<T, ulong>(flags) | Unsafe.BitCast<T, ulong>(with)),
				_ => throw new Exception("Size does not match a known Enum backing type."),
			};

		/// <summary>
		/// Return <paramref name="flags"/> after clearing all the set bits in <paramref name="without"/>
		/// </summary>
		public unsafe T Without(T without)
			=> sizeof(T) switch
			{
				1 => Unsafe.BitCast<byte, T>((byte)(Unsafe.BitCast<T, byte>(flags) & ~Unsafe.BitCast<T, byte>(without))),
				2 => Unsafe.BitCast<ushort, T>((ushort)(Unsafe.BitCast<T, ushort>(flags) & ~Unsafe.BitCast<T, ushort>(without))),
				4 => Unsafe.BitCast<uint, T>(Unsafe.BitCast<T, uint>(flags) & ~Unsafe.BitCast<T, uint>(without)),
				8 => Unsafe.BitCast<ulong, T>(Unsafe.BitCast<T, ulong>(flags) & ~Unsafe.BitCast<T, ulong>(without)),
				_ => throw new Exception("Size does not match a known Enum backing type."),
			};

		/// <summary>
		/// Return <paramref name="flags"/> after setting or clearing all the set bits in <paramref name="mask"/>, depending on the value of <paramref name="condition"/> (set if true, clear if false)
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public T Mask(T mask, bool condition)
			=> condition ? flags.With(mask) : flags.Without(mask);
	}
}

