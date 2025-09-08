using System.Collections;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Foster.Framework;

/// <summary>
/// A 2D Integer Rectangle
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct RectInt(int x, int y, int w, int h) : IConvexShape, IEquatable<RectInt>
{
	public int X = x;
	public int Y = y;
	public int Width = w;
	public int Height = h;

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

	public readonly LineInt LeftLine => new(BottomLeft, TopLeft);
	public readonly LineInt RightLine => new(TopRight, BottomRight);
	public readonly LineInt TopLine => new(TopLeft, TopRight);
	public readonly LineInt BottomLine => new(BottomRight, BottomLeft);

	public EdgeEnumerable Edges => new(this);

	public readonly struct EdgeEnumerable(RectInt rect) : IEnumerable<LineInt>
	{
		public EdgeEnumerator GetEnumerator() => new(rect);
		IEnumerator<LineInt> IEnumerable<LineInt>.GetEnumerator() => new EdgeEnumerator(rect);
		IEnumerator IEnumerable.GetEnumerator() => new EdgeEnumerator(rect);
	}

	public struct EdgeEnumerator(RectInt rect) : IEnumerator<LineInt>
	{
		private int index = -1;
		private LineInt current;

		public bool MoveNext()
		{
			index++;
			if (index < 4)
			{
				current = index switch
				{
					0 => rect.RightLine,
					1 => rect.BottomLine,
					2 => rect.LeftLine,
					_ => rect.TopLine,
				};
				return true;
			}
			else
				return false;
		}

		public void Reset()
		{
			index = -1;
		}

		public LineInt Current => current;
		LineInt IEnumerator<LineInt>.Current => current;
		object IEnumerator.Current => current;
		public void Dispose() { }
	}

	#endregion

	#region Points

	public readonly Point2 Min => new(Math.Min(Left, Right), Math.Min(Top, Bottom));
	public readonly Point2 Max => new(Math.Max(Left, Right), Math.Max(Top, Bottom));

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

	public RectInt(int w, int h)
		: this(0, 0, w, h)
	{

	}

	public RectInt(in Point2 pos, int w, int h)
		: this(pos.X, pos.Y, w, h)
	{

	}

	public RectInt(in Point2 pos, in Point2 size)
		: this(pos.X, pos.Y, size.X, size.Y)
	{

	}

	#region Collision

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

	/// <summary>
	/// Gets the smallest rectangle that fully contains both this and the other rectangle
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly RectInt Conflate(in RectInt other)
	{
		var min = Point2.Min(Min, other.Min);
		var max = Point2.Max(Max, other.Max);
		return new(min.X, min.Y, max.X - min.X, max.Y - min.Y);
	}

	/// <summary>
	/// Gets the smallest rectangle that fully contains both this and the point
	/// </summary>
	public readonly RectInt Conflate(in Point2 other)
		=> Between(Point2.Min(TopLeft, other), Point2.Max(BottomRight, other));

	/// <summary>
	/// Get the largest rectangle full contained by both rectangles
	/// </summary>
	public readonly RectInt GetIntersection(in RectInt against)
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

	/// <summary>
	/// Return the sector that the point falls within (see diagram in comments below). A result of zero indicates a point inside the rectangle
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

	public readonly bool Overlaps(in Line line)
	{
		var secA = GetPointSector(line.From);
		var secB = GetPointSector(line.To);

		if (secA == 0 || secB == 0)
			return true;
		else if ((secA & secB) != 0)
			return false;
		else
		{
			// Do line checks against the edges
			var both = secA | secB;

			// top check
			if ((both & 0b0100) != 0
			&& line.Intersects(new Line(TopLeft, TopRight)))
				return true;

			// bottom check
			if ((both & 0b1000) != 0
			&& line.Intersects(new Line(BottomLeft, BottomRight)))
				return true;

			// left edge check
			if ((both & 0b0001) != 0
			&& line.Intersects(new Line(TopLeft, BottomLeft)))
				return true;

			// right edge check
			if ((both & 0b0010) != 0
			&& line.Intersects(new Line(TopRight, BottomRight)))
				return true;

			return false;
		}
	}

	public readonly bool Overlaps(in LineInt line)
	{
		var secA = GetPointSector(line.From);
		var secB = GetPointSector(line.To);

		if (secA == 0 || secB == 0)
			return true;
		else if ((secA & secB) != 0)
			return false;
		else
		{
			// Do line checks against the edges
			var both = secA | secB;

			// top check
			if ((both & 0b0100) != 0
			&& line.Intersects(new LineInt(TopLeft, TopRight)))
				return true;

			// bottom check
			if ((both & 0b1000) != 0
			&& line.Intersects(new LineInt(BottomLeft, BottomRight)))
				return true;

			// left edge check
			if ((both & 0b0001) != 0
			&& line.Intersects(new LineInt(TopLeft, BottomLeft)))
				return true;

			// right edge check
			if ((both & 0b0010) != 0
			&& line.Intersects(new LineInt(TopRight, BottomRight)))
				return true;

			return false;
		}
	}

	#endregion

	#region Transform

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly RectInt At(in Point2 pos) => new(pos.X, pos.Y, Width, Height);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly RectInt AtX(int x) => new(x, Y, Width, Height);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly RectInt AtY(int y) => new(X, y, Width, Height);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly RectInt Translate(int byX, int byY) => new(X + byX, Y + byY, Width, Height);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly RectInt Translate(in Point2 by) => new(X + by.X, Y + by.Y, Width, Height);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly RectInt Scale(int by) => new RectInt(X * by, Y * by, Width * by, Height * by).ValidateSize();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly RectInt Scale(int byX, int byY) => new RectInt(X * byX, Y * byY, Width * byX, Height * byY).ValidateSize();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly RectInt Scale(in Point2 by) => new RectInt(X * by.X, Y * by.Y, Width * by.X, Height * by.Y).ValidateSize();

	public readonly RectInt ScaleX(int byX)
	{
		var r = new RectInt(X * byX, Y, Width * byX, Height);

		if (r.Width < 0)
		{
			r.X += r.Width;
			r.Width *= -1;
		}

		return r;
	}

	public readonly RectInt ScaleY(int byY)
	{
		var r = new RectInt(X, Y * byY, Width, Height * byY);

		if (r.Height < 0)
		{
			r.Y += r.Height;
			r.Height *= -1;
		}

		return r;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly Rect Scale(float by) => new Rect(X * by, Y * by, Width * by, Height * by).ValidateSize();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly Rect Scale(float byX, int byY) => new Rect(X * byX, Y * byY, Width * byX, Height * byY).ValidateSize();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly Rect Scale(in Vector2 by) => new Rect(X * by.X, Y * by.Y, Width * by.X, Height * by.Y).ValidateSize();

	public readonly Rect ScaleX(float byX)
	{
		var r = new Rect(X * byX, Y, Width * byX, Height);

		if (r.Width < 0)
		{
			r.X += r.Width;
			r.Width *= -1;
		}

		return r;
	}

	public readonly Rect ScaleY(float byY)
	{
		var r = new Rect(X, Y * byY, Width, Height * byY);

		if (r.Height < 0)
		{
			r.Y += r.Height;
			r.Height *= -1;
		}

		return r;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly RectInt Inflate(int by) => new(X - by, Y - by, Width + by * 2, Height + by * 2);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly RectInt Inflate(int byX, int byY) => new(X - byX, Y - byY, Width + byX * 2, Height + byY * 2);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly RectInt Inflate(in Point2 by) => Inflate(by.X, by.Y);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly RectInt InflateX(int byX) => new(X - byX, Y, Width + byX * 2, Height);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly RectInt InflateY(int byY) => new(X, Y - byY, Width, Height + byY * 2);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly Rect Inflate(float by) => new(X - by, Y - by, Width + by * 2, Height + by * 2);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly Rect Inflate(float byX, float byY) => new(X - byX, Y - byY, Width + byX * 2, Height + byY * 2);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly Rect Inflate(in Vector2 by) => Inflate(by.X, by.Y);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly Rect InflateX(float byX) => new(X - byX, Y, Width + byX * 2, Height);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly Rect InflateY(float byY) => new(X, Y - byY, Width, Height + byY * 2);

	public readonly RectInt Inflate(int left, int top, int right, int bottom)
	{
		var rect = new RectInt(X, Y, Width, Height);
		rect.Left -= left;
		rect.Top -= top;
		rect.Width += left + right;
		rect.Height += top + bottom;
		return rect;
	}

	public readonly Rect Inflate(float left, float top, float right, float bottom)
	{
		var rect = new Rect(X, Y, Width, Height);
		rect.Left -= left;
		rect.Top -= top;
		rect.Width += left + right;
		rect.Height += top + bottom;
		return rect;
	}

	public readonly RectInt RotateLeft(in Point2 origin)
	{
		Point2 a = (TopLeft - origin).TurnLeft();
		Point2 b = (TopRight - origin).TurnLeft();
		Point2 c = (BottomRight - origin).TurnLeft();
		Point2 d = (BottomLeft - origin).TurnLeft();
		Point2 min = Point2.Min(a, b, c, d);
		Point2 max = Point2.Max(a, b, c, d);
		return new(min.X, min.Y, max.X - min.X, max.Y - min.Y);
	}

	public readonly RectInt RotateLeft(in Point2 origin, int count)
	{
		RectInt r = this;
		while (count-- > 0)
			r = r.RotateLeft(origin);
		return r;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly RectInt RotateLeft() => RotateLeft(Point2.Zero);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly RectInt RotateLeft(int count) => RotateLeft(Point2.Zero, count);

	public readonly RectInt RotateRight(in Point2 origin)
	{
		Point2 a = (TopLeft - origin).TurnRight();
		Point2 b = (TopRight - origin).TurnRight();
		Point2 c = (BottomRight - origin).TurnRight();
		Point2 d = (BottomLeft - origin).TurnRight();
		Point2 min = Point2.Min(a, b, c, d);
		Point2 max = Point2.Max(a, b, c, d);
		return new(min.X, min.Y, max.X - min.X, max.Y - min.Y);
	}
	public readonly RectInt RotateRight(in Point2 origin, int count)
	{
		RectInt r = this;
		while (count-- > 0)
			r = r.RotateRight(origin);
		return r;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly RectInt RotateRight() => RotateRight(Point2.Zero);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly RectInt RotateRight(int count) => RotateRight(Point2.Zero, count);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly RectInt Rotate(Cardinal direction) => RotateRight(direction.Value);

	/// <summary>
	/// Resolve negative width or height to an equivalent rectangle with positive width and height. Ex: (0, 0, -2, -3) validates to (-2, -3, 2, 3)
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly RectInt ValidateSize()
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

	#endregion

	#region IConvexShape

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

	#endregion

	#region Static Constructors

	/// <summary>
	/// Get a rect centered around a position
	/// </summary>
	public static RectInt Centered(int centerX, int centerY, int width, int height)
		=> new(centerX - width / 2, centerY - height / 2, width, height);

	/// <summary>
	/// Get a rect centered around a position
	/// </summary>
	public static RectInt Centered(in Point2 center, int width, int height)
		=> new(center.X - width / 2, center.Y - height / 2, width, height);

	/// <summary>
	/// Get a rect centered around a position
	/// </summary>
	public static RectInt Centered(in Point2 center, in Point2 size)
		=> new(center.X - size.X / 2, center.Y - size.Y / 2, size.X, size.Y);

	/// <summary>
	/// Get a rect centered around (0, 0)
	/// </summary>
	public static RectInt Centered(in Point2 size)
		=> new(-size.X / 2, -size.Y / 2, size.X, size.Y);

	/// <summary>
	/// Get a rect centered around (0, 0)
	/// </summary>
	public static RectInt Centered(int width, int height)
		=> new(-width / 2, -height / 2, width, height);

	/// <summary>
	/// Get the rect with positive width and height that stretches from point a to point b
	/// </summary>
	public static RectInt Between(in Point2 a, in Point2 b)
	{
		RectInt rect;

		rect.X = a.X < b.X ? a.X : b.X;
		rect.Y = a.Y < b.Y ? a.Y : b.Y;
		rect.Width = (a.X > b.X ? a.X : b.X) - rect.X;
		rect.Height = (a.Y > b.Y ? a.Y : b.Y) - rect.Y;

		return rect;
	}

	#endregion

	#region Enumerate Points

	/// <summary>
	/// Enumerate all integer positions within this rectangle
	/// </summary>
	public readonly PointEnumerable EnumeratePoints => new(this);

	public readonly struct PointEnumerable(RectInt rect) : IEnumerable<Point2>
	{
		public PointEnumerator GetEnumerator() => new(rect);
		IEnumerator<Point2> IEnumerable<Point2>.GetEnumerator() => new PointEnumerator(rect);
		IEnumerator IEnumerable.GetEnumerator() => new EdgeEnumerator(rect);
	}

	public struct PointEnumerator(RectInt rect) : IEnumerator<Point2>
	{
		private readonly int    total = rect.Area;
		private          int    index = -1;
		private          Point2 current;

		public bool MoveNext()
		{
			index++;
			if (index < total)
			{
				current = new(rect.X + index % rect.Width, rect.Y + index / rect.Width);
				return true;
			}
			else
				return false;
		}

		public void Reset()
		{
			index = -1;
		}

		public Point2 Current => current;
		Point2 IEnumerator<Point2>.Current => current;
		object IEnumerator.Current => current;
		public void Dispose() { }
	}

	#endregion

	/// <summary>
	/// Get the rect as a tuple of integers
	/// </summary>
	public readonly (int X, int Y, int Width, int Height) Deconstruct() => (X, Y, Width, Height);

	/// <summary>
	/// Get the rect as a tuple of floats
	/// </summary>
	public readonly void Deconstruct(out int x, out int y, out int width, out int height) => (x, y, width, height) = (X, Y, Width, Height);

	public readonly bool Equals(RectInt other) => this == other;
	public readonly override bool Equals(object? obj) => (obj is RectInt other) && (this == other);
	public readonly override int GetHashCode() => HashCode.Combine(X, Y, Width, Height);
	public readonly override string ToString() => $"[{X}, {Y}, {Width}, {Height}]";

	public static implicit operator RectInt((int X, int Y, int Width, int Height) tuple) => new(tuple.X, tuple.Y, tuple.Width, tuple.Height);
	public static implicit operator Rect(in RectInt rect) => new(rect.X, rect.Y, rect.Width, rect.Height);

	public static bool operator ==(RectInt a, RectInt b) => a.X == b.X && a.Y == b.Y && a.Width == b.Width && a.Height == b.Height;
	public static bool operator !=(RectInt a, RectInt b) => !(a == b);

	public static RectInt operator +(in RectInt a, in Point2 b) => new(a.X + b.X, a.Y + b.Y, a.Width, a.Height);
	public static RectInt operator -(in RectInt a, in Point2 b) => new(a.X - b.X, a.Y - b.Y, a.Width, a.Height);
	public static RectInt operator +(in Point2 a, in RectInt b) => new(b.X + a.X, b.Y + a.Y, b.Width, b.Height);
	public static RectInt operator -(in Point2 a, in RectInt b) => new(b.X - a.X, b.Y - a.Y, b.Width, b.Height);
	public static RectInt operator *(in RectInt rect, int scaler) => rect.Scale(scaler);
	public static RectInt operator /(in RectInt rect, int scaler)
		=> new RectInt(rect.X / scaler, rect.Y / scaler, rect.Width / scaler, rect.Height / scaler).ValidateSize();
	public static RectInt operator *(in RectInt rect, in Point2 scaler) => rect.Scale(scaler);
	public static RectInt operator /(in RectInt rect, in Point2 scaler)
		=> new RectInt(rect.X / scaler.X, rect.Y / scaler.Y, rect.Width / scaler.X, rect.Height / scaler.Y).ValidateSize();
	public static RectInt operator *(in RectInt rect, Cardinal rotation) => rect.Rotate(rotation);
	
	// TODO: remove once Facing is deleted
#pragma warning disable 0618
	public static RectInt operator *(in RectInt rect, Facing flipX) => flipX == Facing.Right ? rect : rect.ScaleX(-1);
#pragma warning restore 0618

	public static Rect operator +(in RectInt a, in Vector2 b) => new(a.X + b.X, a.Y + b.Y, a.Width, a.Height);
	public static Rect operator -(in RectInt a, in Vector2 b) => new(a.X - b.X, a.Y - b.Y, a.Width, a.Height);
	public static Rect operator +(in Vector2 a, in RectInt b) => new(b.X + a.X, b.Y + a.Y, b.Width, b.Height);
	public static Rect operator -(in Vector2 a, in RectInt b) => new(b.X - a.X, b.Y - a.Y, b.Width, b.Height);
	public static Rect operator *(in RectInt rect, float scaler) => rect.Scale(scaler);
	public static Rect operator /(in RectInt rect, float scaler) => new Rect(rect.X / scaler, rect.Y / scaler, rect.Width / scaler, rect.Height / scaler).ValidateSize();
	public static Rect operator *(in RectInt rect, in Vector2 scaler) => rect.Scale(scaler);
	public static Rect operator /(in RectInt rect, in Vector2 scaler) => new Rect(rect.X / scaler.X, rect.Y / scaler.Y, rect.Width / scaler.X, rect.Height / scaler.Y).ValidateSize();

}
