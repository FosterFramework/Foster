using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Foster.Framework;

/// <summary>
/// A 2D Integer Rectangle
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct RectInt : IEquatable<RectInt>
{
	public int X;
	public int Y;
	public int Width;
	public int Height;

	public Point2 Position
	{
		readonly get => new(X, Y);
		set
		{
			X = value.X;
			Y = value.Y;
		}
	}

	public Point2 Size
	{
		readonly get => new(Width, Height);
		set
		{
			Width = value.X;
			Height = value.Y;
		}
	}

	public readonly int Area => Width * Height;

	#region Edges

	public int Left
	{
		readonly get => X;
		set => X = value;
	}

	public int Right
	{
		readonly get => X + Width;
		set => X = value - Width;
	}

	public int CenterX
	{
		readonly get => X + Width / 2;
		set => X = value - Width / 2;
	}

	public int Top
	{
		readonly get => Y;
		set => Y = value;
	}

	public int Bottom
	{
		readonly get => Y + Height;
		set => Y = value - Height;
	}

	public int CenterY
	{
		readonly get => Y + Height / 2;
		set => Y = value - Height / 2;
	}

	#endregion

	#region Points

	public readonly Point2 Min => new(Math.Min(X, Right), Math.Min(Y, Bottom));

	public readonly Point2 Max => new(Math.Max(X, Right), Math.Max(Y, Bottom));

	public Point2 TopLeft
	{
		readonly get => new(Left, Top);
		set
		{
			Left = value.X;
			Top = value.Y;
		}
	}

	public Point2 TopCenter
	{
		readonly get => new(CenterX, Top);
		set
		{
			CenterX = value.X;
			Top = value.Y;
		}
	}

	public Point2 TopRight
	{
		readonly get => new(Right, Top);
		set
		{
			Right = value.X;
			Top = value.Y;
		}
	}

	public Point2 CenterLeft
	{
		readonly get => new(Left, CenterY);
		set
		{
			Left = value.X;
			CenterY = value.Y;
		}
	}

	public Point2 Center
	{
		readonly get => new(CenterX, CenterY);
		set
		{
			CenterX = value.X;
			CenterY = value.Y;
		}
	}

	public Point2 CenterRight
	{
		readonly get => new(Right, CenterY);
		set
		{
			Right = value.X;
			CenterY = value.Y;
		}
	}

	public Point2 BottomLeft
	{
		readonly get => new(Left, Bottom);
		set
		{
			Left = value.X;
			Bottom = value.Y;
		}
	}

	public Point2 BottomCenter
	{
		readonly get => new(CenterX, Bottom);
		set
		{
			CenterX = value.X;
			Bottom = value.Y;
		}
	}

	public Point2 BottomRight
	{
		readonly get => new(Right, Bottom);
		set
		{
			Right = value.X;
			Bottom = value.Y;
		}
	}

	#endregion

	#region PointsF

	public readonly float CenterXF => X + Width * .5f;
	public readonly float CenterYF => Y + Height * .5f;
	public readonly Vector2 TopCenterF => new(CenterXF, Top);
	public readonly Vector2 CenterLeftF => new(Left, CenterYF);
	public readonly Vector2 CenterF => new(CenterXF, CenterYF);
	public readonly Vector2 CenterRightF => new(Right, CenterYF);
	public readonly Vector2 BottomCenterF => new(CenterXF, Bottom);

	#endregion

	public RectInt(int x, int y, int w, int h)
	{
		X = x;
		Y = y;
		Width = w;
		Height = h;
	}

	public RectInt(int w, int h) : this(0, 0, w, h) { }

	public RectInt(in Point2 position, in Point2 size)
	{
		X = position.X;
		Y = position.Y;
		Width = size.X;
		Height = size.Y;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly bool Contains(in Point2 point)
		=> (point.X >= X && point.Y >= Y && point.X < X + Width && point.Y < Y + Height);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly bool Contains(in Vector2 vec)
		=> (vec.X >= X && vec.Y >= Y && vec.X < X + Width && vec.Y < Y + Height);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly bool Contains(in RectInt rect)
		=> (Left < rect.Left && Top < rect.Top && Bottom > rect.Bottom && Right > rect.Right);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly bool Overlaps(in RectInt against)
		=> X + Width > against.X && Y + Height > against.Y && X < against.X + against.Width && Y < against.Y + against.Height;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly bool Overlaps(in Rect against)
		=> X + Width > against.X && Y + Height > against.Y && X < against.X + against.Width && Y < against.Y + against.Height;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly RectInt Conflate(in RectInt other)
	{
		var min = Point2.Min(Min, other.Min);
		var max = Point2.Max(Max, other.Max);
		return new(min.X, min.Y, max.X - min.X, max.Y - min.Y);
	}

	public readonly RectInt Inflate(int by)
		=> new(X - by, Y - by, Width + by * 2, Height + by * 2);

	public readonly RectInt Inflate(int byX, int byY)
		=> new(X - byX, Y - byY, Width + byX * 2, Height + byY * 2);

	public readonly RectInt Inflate(in Point2 by)
		=> Inflate(by.X, by.Y);

	public readonly Rect Inflate(float by)
		=> new(X - by, Y - by, Width + by * 2, Height + by * 2);

	public readonly Rect Inflate(float byX, float byY)
		=> new(X - byX, Y - byY, Width + byX * 2, Height + byY * 2);

	public readonly Rect Inflate(in Vector2 by)
		=> Inflate(by.X, by.Y);

	public readonly RectInt Translate(int byX, int byY)
		=> new(X + byX, Y + byY, Width, Height);

	public readonly RectInt Translate(in Point2 by)
		=> new(X + by.X, Y + by.Y, Width, Height);

	public readonly RectInt MultiplyX(int scale)
	{
		var r = new RectInt(X * scale, Y, Width * scale, Height);

		if (r.Width < 0)
		{
			r.X += r.Width;
			r.Width *= -1;
		}

		return r;
	}

	public readonly RectInt MultiplyY(int scale)
	{
		var r = new RectInt(X, Y * scale, Width, Height * scale);

		if (r.Height < 0)
		{
			r.Y += r.Height;
			r.Height *= -1;
		}

		return r;
	}

	public readonly RectInt OverlapRect(in RectInt against)
	{
		bool overlapX = X + Width > against.X && X < against.X + against.Width;
		bool overlapY = Y + Height > against.Y && Y < against.Y + against.Height;

		RectInt r = new();

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

	public readonly RectInt RotateLeft(Point2 origin)
	{
		Point2 a = (TopLeft - origin).TurnLeft();
		Point2 b = (TopRight - origin).TurnLeft();
		Point2 c = (BottomRight - origin).TurnLeft();
		Point2 d = (BottomLeft - origin).TurnLeft();
		Point2 min = Point2.Min(a, b, c, d);
		Point2 max = Point2.Max(a, b, c, d);
		return new(min.X, min.Y, max.X - min.X, max.Y - min.Y);
	}
	public readonly RectInt RotateLeft(Point2 origin, int count)
	{
		RectInt r = this;
		while (count-- > 0)
			r = r.RotateLeft(origin);
		return r;
	}
	public readonly RectInt RotateLeft() => RotateLeft(Point2.Zero);
	public readonly RectInt RotateLeft(int count) => RotateLeft(Point2.Zero, count);

	public readonly RectInt RotateRight(Point2 origin)
	{
		Point2 a = (TopLeft - origin).TurnRight();
		Point2 b = (TopRight - origin).TurnRight();
		Point2 c = (BottomRight - origin).TurnRight();
		Point2 d = (BottomLeft - origin).TurnRight();
		Point2 min = Point2.Min(a, b, c, d);
		Point2 max = Point2.Max(a, b, c, d);
		return new(min.X, min.Y, max.X - min.X, max.Y - min.Y);
	}
	public readonly RectInt RotateRight(Point2 origin, int count)
	{
		RectInt r = this;
		while (count-- > 0)
			r = r.RotateRight(origin);
		return r;
	}
	public readonly RectInt RotateRight() => RotateRight(Point2.Zero);
	public readonly RectInt RotateRight(int count) => RotateRight(Point2.Zero, count);

	public readonly RectInt GetSweep(Cardinal direction, int distance)
	{
		if (distance < 0)
		{
			distance *= -1;
			direction = direction.Reverse;
		}

		if (direction == Cardinal.Right)
			return new(X + Width, Y, distance, Height);
		else if (direction == Cardinal.Left)
			return new(X - distance, Y, distance, Height);
		else if (direction == Cardinal.Down)
			return new(X, Y + Height, Width, distance);
		else
			return new(X, Y - distance, Width, distance);
	}

	public override readonly bool Equals(object? obj) => (obj is RectInt other) && (this == other);
	public readonly bool Equals(RectInt other) => this == other;

	public override readonly int GetHashCode()
	{
		int hash = 17;
		hash = hash * 23 + X;
		hash = hash * 23 + Y;
		hash = hash * 23 + Width;
		hash = hash * 23 + Height;
		return hash;
	}

	public override readonly string ToString()
		=> $"[{X}, {Y}, {Width}, {Height}]";

	public static RectInt Box(Point2 center, Point2 size)
		=> new(center.X - size.X / 2, center.Y - size.Y / 2, size.X, size.Y);

	public static RectInt Between(Point2 a, Point2 b)
	{
		RectInt rect;

		rect.X = a.X < b.X ? a.X : b.X;
		rect.Y = a.Y < b.Y ? a.Y : b.Y;
		rect.Width = (a.X > b.X ? a.X : b.X) - rect.X;
		rect.Height = (a.Y > b.Y ? a.Y : b.Y) - rect.Y;

		return rect;
	}

	public static implicit operator RectInt((int X, int Y, int Width, int Height) tuple)
		=> new(tuple.X, tuple.Y, tuple.Width, tuple.Height);

	public static bool operator ==(RectInt a, RectInt b)
		=> a.X == b.X && a.Y == b.Y && a.Width == b.Width && a.Height == b.Height;

	public static bool operator !=(RectInt a, RectInt b)
		=> !(a == b);

	public static RectInt operator *(RectInt rect, Facing flipX)
	{
		if (flipX == Facing.Right)
			return rect;
		else
			return rect.MultiplyX(-1);
	}

	public static RectInt operator *(in RectInt rect, int scaler)
		=> new RectInt(rect.X * scaler, rect.Y * scaler, rect.Width * scaler, rect.Height * scaler).Validate();

	public static RectInt operator *(in RectInt rect, in Point2 scaler)
		=> new RectInt(rect.X * scaler.X, rect.Y * scaler.Y, rect.Width * scaler.X, rect.Height * scaler.Y).Validate();

	public static RectInt operator /(in RectInt rect, int scaler)
		=> new RectInt(rect.X / scaler, rect.Y / scaler, rect.Width / scaler, rect.Height / scaler).Validate();

	public static RectInt operator /(in RectInt rect, in Point2 scaler)
		=> new RectInt(rect.X / scaler.X, rect.Y / scaler.Y, rect.Width / scaler.X, rect.Height / scaler.Y).Validate();

	public static RectInt operator +(in RectInt a, in Point2 b)
		=> new(a.X + b.X, a.Y + b.Y, a.Width, a.Height);

	public static RectInt operator -(in RectInt a, in Point2 b)
		=> new(a.X - b.X, a.Y - b.Y, a.Width, a.Height);

	public static explicit operator RectInt(in Rect rect)
		=> new((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height);

	public static Rect operator *(in RectInt rect, float scaler)
		=> new Rect(rect.X * scaler, rect.Y * scaler, rect.Width * scaler, rect.Height * scaler).Validate();

	public static Rect operator *(in RectInt rect, in Vector2 scaler)
		=> new Rect(rect.X * scaler.X, rect.Y * scaler.Y, rect.Width * scaler.X, rect.Height * scaler.Y).Validate();

	public static Rect operator /(in RectInt rect, float scaler)
		=> new Rect(rect.X / scaler, rect.Y / scaler, rect.Width / scaler, rect.Height / scaler).Validate();

	public static Rect operator /(in RectInt rect, in Vector2 scaler)
		=> new Rect(rect.X / scaler.X, rect.Y / scaler.Y, rect.Width / scaler.X, rect.Height / scaler.Y).Validate();


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private readonly RectInt Validate()
	{
		RectInt rect = this;

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

	public readonly IEnumerable<Point2> AllPoints
	{
		get
		{
			for (int x = X; x < X + Width; x++)
				for (int y = Y; y < Y + Height; y++)
					yield return new(x, y);
		}
	}
}
