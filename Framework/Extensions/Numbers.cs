using System.Numerics;
using System.Runtime.CompilerServices;

namespace Foster.Framework;

public static partial class Extensions
{
	/// <summary>
	/// Bitwise Flag Operations
	/// </summary>
	extension<T>(T flags) where T : IEqualityOperators<T, T, bool>, IBitwiseOperators<T, T, T>
	{
		/// <summary>
		/// Bitwise check if <paramref name="flags"/> has set any of the set bits in <paramref name="check"/>
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Has(T check)
			=> (flags & check) != default;

		/// <summary>
		/// Bitwise check if <paramref name="flags"/> has set all of the set bits in <paramref name="check"/>
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool HasAll(T check)
			=> (flags & check) == check;

		/// <summary>
		/// Return <paramref name="flags"/> after setting all the set bits in <paramref name="with"/>
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public T With(T with)
			=> (T)(flags | with);

		/// <summary>
		/// Return <paramref name="flags"/> after clearing all the set bits in <paramref name="without"/>
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public T Without(T without)
			=> (T)(flags & ~without);

		/// <summary>
		/// Return <paramref name="flags"/> after setting or clearing all the set bits in <paramref name="mask"/>, depending on the value of <paramref name="condition"/> (set if true, clear if false)
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public T Mask(T mask, bool condition)
			=> condition ? flags.With(mask) : flags.Without(mask);
	}
}