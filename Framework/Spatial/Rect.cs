using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Foster.Framework;

/// <summary>
/// A 2D Rectangle
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct Rect : IConvexShape, IEquatable<Rect>
{
	public float X;
	public float Y;
	public float Width;
	public float Height;

	public Vector2 Position
	{
		readonly get => new(X, Y);
		set
		{
			X = value.X;
			Y = value.Y;
		}
	}

	public Vector2 Size
	{
		readonly get => new(Width, Height);
		set
		{
			Width = value.X;
			Height = value.Y;
		}
	}

	public readonly float Area => Math.Abs(Width * Height);

	#region Edges

	public float Left
	{
		readonly get => X;
		set => X = value;
	}

	public float Right
	{
		readonly get => X + Width;
		set => X = value - Width;
	}

	public float CenterX
	{
		readonly get => X + Width / 2;
		set => X = value - Width / 2;
	}

	public float Top
	{
		readonly get => Y;
		set => Y = value;
	}

	public float Bottom
	{
		readonly get => Y + Height;
		set => Y = value - Height;
	}

	public float CenterY
	{
		readonly get => Y + Height / 2;
		set => Y = value - Height / 2;
	}

	public readonly Line LeftLine => new(BottomLeft, TopLeft);
	public readonly Line RightLine => new(TopRight, BottomRight);
	public readonly Line TopLine => new(TopLeft, TopRight);
	public readonly Line BottomLine => new(BottomRight, BottomLeft);

	#endregion

	#region Points

	public Vector2 TopLeft
	{
		readonly get => new(Left, Top);
		set
		{
			Left = value.X;
			Top = value.Y;
		}
	}

	public Vector2 TopCenter
	{
		readonly get => new(CenterX, Top);
		set
		{
			CenterX = value.X;
			Top = value.Y;
		}
	}

	public Vector2 TopRight
	{
		readonly get => new(Right, Top);
		set
		{
			Right = value.X;
			Top = value.Y;
		}
	}

	public Vector2 CenterLeft
	{
		readonly get => new(Left, CenterY);
		set
		{
			Left = value.X;
			CenterY = value.Y;
		}
	}

	public Vector2 Center
	{
		readonly get => new(CenterX, CenterY);
		set
		{
			CenterX = value.X;
			CenterY = value.Y;
		}
	}

	public Vector2 CenterRight
	{
		readonly get => new(Right, CenterY);
		set
		{
			Right = value.X;
			CenterY = value.Y;
		}
	}

	public Vector2 BottomLeft
	{
		readonly get => new(Left, Bottom);
		set
		{
			Left = value.X;
			Bottom = value.Y;
		}
	}

	public Vector2 BottomCenter
	{
		readonly get => new(CenterX, Bottom);
		set
		{
			CenterX = value.X;
			Bottom = value.Y;
		}
	}

	public Vector2 BottomRight
	{
		readonly get => new(Right, Bottom);
		set
		{
			Right = value.X;
			Bottom = value.Y;
		}
	}

	/// <summary>
	/// Get a point on the rectangle based on x- and y-values 0-1 where 0 is the left/top and 1 is the right/bottom
	/// </summary>
	public readonly Vector2 On(float x, float y)
		=> new(X + Width * x, Y + Height * y);

	/// <summary>
	/// Get a point on the rectangle based on x- and y-values 0-1 where 0 is the left/top and 1 is the right/bottom
	/// </summary>
	public readonly Vector2 On(in Vector2 vec)
	   => new(X + Width * vec.X, Y + Height * vec.Y);

	#endregion

	public Rect(float x, float y, float w, float h)
	{
		X = x;
		Y = y;
		Width = w;
		Height = h;
	}

	public Rect(float w, float h)
		: this(0, 0, w, h)
	{

	}

	public Rect(in Vector2 from, in Vector2 to)
	{
		X = Math.Min(from.X, to.X);
		Y = Math.Min(from.Y, to.Y);
		Width = Math.Max(from.X, to.X) - X;
		Height = Math.Max(from.Y, to.Y) - Y;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly bool Contains(in Vector2 point)
		=> point.X >= X && point.Y >= Y && point.X < X + Width && point.Y < Y + Height;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly bool Contains(in Rect rect)
		=> Left <= rect.Left && Top <= rect.Top && Bottom >= rect.Bottom && Right >= rect.Right;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly bool Overlaps(in Rect against)
		=> X + Width > against.X && Y + Height > against.Y && X < against.X + against.Width && Y < against.Y + against.Height;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly bool Overlaps(in Triangle tri)
		=> tri.Contains(TopLeft) || Overlaps(tri.AB) || Overlaps(tri.BC) || Overlaps(tri.CA);

	public readonly bool Overlaps(in Line line)
	{
		var sectorA = GetPointSector(line.From);
		var sectorB = GetPointSector(line.To);

		if ((sectorA & sectorB) != 0)
			return false;	// the two points share an x- or y-sector of the rect so will not cross it

		var combined = sectorA | sectorB;

		if (combined == 0)
			return true;	// at least one of the points is contained by the rect

		return combined switch
		{
			// states where the line must cross the rect
			0b1100 or 0b0011 or 0b1111 or
			0b0111 or 0b1011 or 0b1101 or 0b1110 => true,

			// states where we are crossing a corner and it might intersect the rect
			0b1010 or 0b1001 => line.Intersects(BottomLine),
			0b0110 or 0b0101 => line.Intersects(TopLine),

			_ => false,
		};
	}

	public readonly Rect OverlapRect(in Rect against)
	{
		var overlapX = X + Width > against.X && X < against.X + against.Width;
		var overlapY = Y + Height > against.Y && Y < against.Y + against.Height;

		Rect r = new();

		if (overlapX)
		{
			r.Left = Math.Max(Left, against.Left);
			r.Width = Math.Min(Right, against.Right) - r.Left;
		}

		if (overlapY)
		{
			r.Top = Math.Max(Top, against.Top);
			r.Height = Math.Min(Bottom, against.Bottom) - r.Top;
		}

		return r;
	}

	public readonly RectInt Int()
		=> new((int)X, (int)Y, (int)Width, (int)Height);

	public readonly Rect Inflate(float by)
		=> new(X - by, Y - by, Width + by * 2, Height + by * 2);

	public readonly Rect Inflate(float x, float y)
		=> new(X - x, Y - y, Width + x * 2, Height + y * 2);

	public readonly Rect Inflate(float left, float top, float right, float bottom)
	{
		var rect = new Rect(X, Y, Width, Height);
		rect.Left -= left;
		rect.Top -= top;
		rect.Width += left + right;
		rect.Height += top + bottom;
		return rect;
	}

	public readonly Rect Scale(float by)
		=> new(X * by, Y * by, Width * by, Height * by);

	public readonly Rect Scale(in Vector2 by)
		=> new(X * by.X, Y * by.Y, Width * by.X, Height * by.Y);

	public readonly Rect Translate(float byX, float byY)
		=> new(X + byX, Y + byY, Width, Height);

	public readonly Rect Translate(in Vector2 by)
		=> new(X + by.X, Y + by.Y, Width, Height);

	public readonly void Project(in Vector2 axis, out float min, out float max)
	{
		min = float.MaxValue;
		max = float.MinValue;

		var dot = Vector2.Dot(new(X, Y), axis);
		min = Math.Min(dot, min);
		max = Math.Max(dot, max);
		dot = Vector2.Dot(new(X + Width, Y), axis);
		min = Math.Min(dot, min);
		max = Math.Max(dot, max);
		dot = Vector2.Dot(new(X + Width, Y + Height), axis);
		min = Math.Min(dot, min);
		max = Math.Max(dot, max);
		dot = Vector2.Dot(new(X, Y + Height), axis);
		min = Math.Min(dot, min);
		max = Math.Max(dot, max);
	}

	/// <summary>
	/// Return the sector that the point falls within (see diagram in comments below)
	/// </summary>
	//  0101 | 0100 | 0110
	// ------+------+------
	//  0001 | 0000 | 0010
	// ------+------+------
	//  1001 | 1000 | 1010
	public readonly byte GetPointSector(in Vector2 pt)
	{
		byte sector = 0;
		if (pt.X < X)
			sector |= 0b0001;
		else if (pt.X >= X + Width)
			sector |= 0b0010;
		if (pt.Y < Y)
			sector |= 0b0100;
		else if (pt.Y >= Y + Height)
			sector |= 0b1000;
		return sector;
	}

	public readonly Vector2 ClosestPoint(in Vector2 pt)
		=> GetPointSector(pt) switch
		{
			// left of rect
			0b0001 => new(X, pt.Y),
			// right of rect
			0b0010 => new(X + Width, pt.Y),
			// above rect
			0b0100 => new(pt.X, Y),
			// below rect
			0b1000 => new(pt.X, Y + Height),
			// above & left of rect
			0b0101 => TopLeft,
			// above & right of rect
			0b0110 => TopRight,
			// below & left of rect
			0b1001 => BottomLeft,
			// below & right of rect
			0b1010 => BottomRight,
			_ => pt,
		};

	public readonly int Points => 4;

	public readonly Vector2 GetPoint(int index)
		=> index switch
		{
			0 => TopLeft,
			1 => TopRight,
			2 => BottomRight,
			3 => BottomLeft,
			_ => throw new IndexOutOfRangeException(),
		};

	public readonly int Axes => 2;

	public readonly Vector2 GetAxis(int index)
		=> index switch
		{
			0 => Vector2.UnitX,
			1 => Vector2.UnitY,
			_ => throw new IndexOutOfRangeException(),
		};

	public readonly bool Equals(Rect other)
		=> this == other;

	public readonly override bool Equals(object? obj)
		=> (obj is Rect other) && (this == other);

	public readonly override int GetHashCode()
		=> HashCode.Combine(X, Y, Width, Height);

	public readonly override string ToString()
		=> $"[{X}, {Y}, {Width}, {Height}]";

	public static Rect Between(in Vector2 a, in Vector2 b)
	{
		Rect rect;

		rect.X = a.X < b.X ? a.X : b.X;
		rect.Y = a.Y < b.Y ? a.Y : b.Y;
		rect.Width = (a.X > b.X ? a.X : b.X) - rect.X;
		rect.Height = (a.Y > b.Y ? a.Y : b.Y) - rect.Y;

		return rect;
	}

	public static Rect Centered(in Vector2 center, float width, float height)
		=> new(center.X - width / 2, center.Y - height / 2, width, height);
	
	public static Rect Transform(in Rect rect, in Matrix3x2 matrix)
	{
		var a = Vector2.Transform(rect.TopLeft, matrix);
		var b = Vector2.Transform(rect.TopRight, matrix);
		var c = Vector2.Transform(rect.BottomRight, matrix);
		var d = Vector2.Transform(rect.BottomLeft, matrix);
		var min = new Vector2(Calc.Min(a.X, b.X, c.X, d.X), Calc.Min(a.Y, b.Y, c.Y, d.Y));
		var max = new Vector2(Calc.Max(a.X, b.X, c.X, d.X), Calc.Max(a.Y, b.Y, c.Y, d.Y));
		return new(min.X, min.Y, max.X - min.X, max.Y - min.Y);
	}

	public static implicit operator Rect((float X, float Y, float Width, float Height) tuple) => new(tuple.X, tuple.Y, tuple.Width, tuple.Height);

	public static implicit operator Rect(in RectInt rect)
		=> new(rect.X, rect.Y, rect.Width, rect.Height);

	public static implicit operator Rect(in Vector4 vec)
		=> new(vec.X, vec.Y, vec.Z, vec.W);

	public static implicit operator Vector4(in Rect rect)
		=> new(rect.X, rect.Y, rect.Width, rect.Height);

	public static bool operator ==(in Rect a, in Rect b)
		=> a.X == b.X && a.Y == b.Y && a.Width == b.Width && a.Height == b.Height;

	public static bool operator !=(in Rect a, in Rect b)
		=> !(a == b);

	public static Rect operator *(in Rect a, in Vector2 scaler)
		=> new(a.X * scaler.X, a.Y * scaler.Y, a.Width * scaler.X, a.Height * scaler.Y);

	public static Rect operator +(in Rect a, in Vector2 b)
		=> new(a.X + b.X, a.Y + b.Y, a.Width, a.Height);

	public static Rect operator -(in Rect a, in Vector2 b)
		=> new(a.X - b.X, a.Y - b.Y, a.Width, a.Height);

	public static Rect operator *(in Rect a, float scaler)
		=> new(a.X * scaler, a.Y * scaler, a.Width * scaler, a.Height * scaler);

	public static Rect operator /(in Rect a, float scaler)
		=> new(a.X / scaler, a.Y / scaler, a.Width / scaler, a.Height / scaler);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly Rect Validate()
	{
		var rect = this;

		if (Width < 0)
		{
			rect.X += Width;
			rect.Width *= -1;
		}

		if (Height < 0)
		{
			rect.Y += Height;
			rect.Height *= -1;
		}

		return rect;
	}
}
