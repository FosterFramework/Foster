using System.Runtime.InteropServices;

namespace Foster.Framework;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct BlendMode : IEquatable<BlendMode>
{
	public BlendOp ColorOperation;
	public BlendFactor ColorSource;
	public BlendFactor ColorDestination;
	public BlendOp AlphaOperation;
	public BlendFactor AlphaSource;
	public BlendFactor AlphaDestination;
	public BlendMask Mask;
	public Color Color;

	public BlendMode(BlendOp operation, BlendFactor source, BlendFactor destination)
	{
		ColorOperation = AlphaOperation = operation;
		ColorSource = AlphaSource = source;
		ColorDestination = AlphaDestination = destination;
		Mask = BlendMask.RGBA;
		Color = Color.White;
	}

	public BlendMode(
		BlendOp colorOperation, BlendFactor colorSource, BlendFactor colorDestination, 
		BlendOp alphaOperation, BlendFactor alphaSource, BlendFactor alphaDestination, 
		BlendMask mask, Color color)
	{
		ColorOperation = colorOperation;
		ColorSource = colorSource;
		ColorDestination = colorDestination;
		AlphaOperation = alphaOperation;
		AlphaSource = alphaSource;
		AlphaDestination = alphaDestination;
		Mask = mask;
		Color = color;
	}

	public static readonly BlendMode Premultiply
		= new (BlendOp.Add, BlendFactor.One, BlendFactor.OneMinusSrcAlpha);
	public static readonly BlendMode NonPremultiplied
		= new(BlendOp.Add, BlendFactor.SrcAlpha, BlendFactor.OneMinusSrcAlpha);
	public static readonly BlendMode Add
		= new (BlendOp.Add, BlendFactor.One, BlendFactor.DstAlpha);
	public static readonly BlendMode Subtract
		= new (BlendOp.ReverseSubtract, BlendFactor.One, BlendFactor.One);
	public static readonly BlendMode Multiply
		= new (BlendOp.Add, BlendFactor.DstColor, BlendFactor.OneMinusSrcAlpha);
	public static readonly BlendMode Screen
		= new (BlendOp.Add, BlendFactor.One, BlendFactor.OneMinusSrcColor);

	public static bool operator ==(BlendMode a, BlendMode b)
		=>  a.ColorOperation == b.ColorOperation &&
			a.ColorSource == b.ColorSource &&
			a.ColorDestination == b.ColorDestination &&
			a.AlphaOperation == b.AlphaOperation &&
			a.AlphaSource == b.AlphaSource &&
			a.AlphaDestination == b.AlphaDestination &&
			a.Mask == b.Mask &&
			a.Color == b.Color;

	public static bool operator !=(BlendMode a, BlendMode b)
		=> !(a == b);

	public override readonly bool Equals(object? obj)
		=> (obj is BlendMode mode) && (this == mode);

	public readonly bool Equals(BlendMode other)
		=> (this == other);

	public readonly override int GetHashCode()
		=> HashCode.Combine(
			ColorOperation,
			ColorSource,
			ColorDestination,
			AlphaOperation,
			AlphaSource,
			AlphaDestination,
			Mask,
			Color);
}
