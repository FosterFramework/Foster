using System.Collections;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Foster.Framework;

/// <summary>
/// A 2D Floating-Point Rectangle
/// </summary>
[StructLayout(LayoutKind.Sequential), JsonConverter(typeof(JsonConverter))]
public struct Rect(float x, float y, float w, float h) : IConvexShape, IEquatable<Rect>
{
	public float X = x;
	public float Y = y;
	public float Width = w;
	public float Height = h;

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

	public EdgeEnumerable Edges => new(this);

	public readonly struct EdgeEnumerable(Rect rect) : IEnumerable<Line>
	{
		public EdgeEnumerator GetEnumerator() => new(rect);
		IEnumerator<Line> IEnumerable<Line>.GetEnumerator() => new EdgeEnumerator(rect);
		IEnumerator IEnumerable.GetEnumerator() => new EdgeEnumerator(rect);
	}

	public struct EdgeEnumerator(Rect rect) : IEnumerator<Line>
	{
		private int index = -1;
		private Line current;

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

		public Line Current => current;
		Line IEnumerator<Line>.Current => current;
		object IEnumerator.Current => current;
		public void Dispose() { }
	}

	#endregion

	#region Points

	public readonly Vector2 Min => new(Math.Min(Left, Right), Math.Min(Top, Bottom));
	public readonly Vector2 Max => new(Math.Max(Left, Right), Math.Max(Top, Bottom));

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

	/// <summary>
	/// Get a random point that lies inside the rectangle
	/// </summary>
	public readonly Vector2 RandomPoint(ref Rng rng)
		=> On(rng.Float(), rng.Float());

	#endregion

	public Rect(float w, float h)
		: this(0, 0, w, h)
	{

	}

	public Rect(in Vector2 pos, float w, float h)
		: this(pos.X, pos.Y, w, h)
	{

	}

	public Rect(in Vector2 pos, in Vector2 size)
		: this(pos.X, pos.Y, size.X, size.Y)
	{

	}

	#region Collision

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

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly bool Overlaps(in Line line) => this.Overlaps(line, out _);

	/// <summary>
	/// Get the largest rectangle full contained by both rectangles
	/// </summary>
	public readonly Rect GetIntersection(in Rect against)
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

	#endregion

	#region Transform

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly RectInt Int() => new((int)X, (int)Y, (int)Width, (int)Height);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly Rect At(in Vector2 pos) => new(pos.X, pos.Y, Width, Height);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly Rect AtX(float x) => new(x, Y, Width, Height);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly Rect AtY(float y) => new(X, y, Width, Height);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly Rect Translate(float byX, float byY) => new(X + byX, Y + byY, Width, Height);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly Rect Translate(in Vector2 by) => new(X + by.X, Y + by.Y, Width, Height);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly Rect Inflate(float by) => new(X - by, Y - by, Width + by * 2, Height + by * 2);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly Rect Inflate(float byX, float byY) => new(X - byX, Y - byY, Width + byX * 2, Height + byY * 2);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly Rect InflateX(float byX) => new(X - byX, Y, Width + byX * 2, Height);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly Rect InflateY(float byY) => new(X, Y - byY, Width, Height + byY * 2);

	public readonly Rect Inflate(float left, float top, float right, float bottom)
	{
		var rect = new Rect(X, Y, Width, Height);
		rect.Left -= left;
		rect.Top -= top;
		rect.Width += left + right;
		rect.Height += top + bottom;
		return rect;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly Rect Scale(float by) => new Rect(X * by, Y * by, Width * by, Height * by).ValidateSize();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly Rect Scale(float byX, float byY) => new Rect(X * byX, Y * byY, Width * byX, Height * byY).ValidateSize();

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

	/// <summary>
	/// Resolve negative width or height to an equivalent rectangle with positive width and height. Ex: (0, 0, -2, -3) validates to (-2, -3, 2, 3)
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly Rect ValidateSize()
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

	public readonly Quad Transform(in Matrix3x2 matrix)
		=> new(
			Vector2.Transform(TopLeft, matrix),
			Vector2.Transform(TopRight, matrix),
			Vector2.Transform(BottomRight, matrix),
			Vector2.Transform(BottomLeft, matrix)
			);

	/// <summary>
	/// Gets the smallest rectangle that contains both this and another rectangle
	/// </summary>
	public readonly Rect Conflate(in Rect other)
		=> Between(Vector2.Min(TopLeft, other.TopLeft), Vector2.Max(BottomRight, other.BottomRight));

	/// <summary>
	/// Gets the smallest rectangle that contains both this and the point
	/// </summary>
	public readonly Rect Conflate(in Vector2 other)
		=> Between(Vector2.Min(TopLeft, other), Vector2.Max(BottomRight, other));

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
	public static Rect Centered(float centerX, float centerY, float width, float height)
		=> new(centerX - width / 2, centerY - height / 2, width, height);

	/// <summary>
	/// Get a rect centered around a position
	/// </summary>
	public static Rect Centered(in Vector2 center, float width, float height)
		=> new(center.X - width / 2, center.Y - height / 2, width, height);

	/// <summary>
	/// Get a rect centered around a position
	/// </summary>
	public static Rect Centered(in Vector2 center, in Vector2 size)
		=> new(center.X - size.X / 2, center.Y - size.Y / 2, size.X, size.Y);

	/// <summary>
	/// Get a rect centered around (0, 0)
	/// </summary>
	public static Rect Centered(in Vector2 size)
		=> new(-size.X/2, -size.Y/2, size.X, size.Y);

	/// <summary>
	/// Get a rect centered around (0, 0)
	/// </summary>
	public static Rect Centered(float width, float height)
		=> new(-width/2, -height/2, width, height);

	/// <summary>
	/// Get a rect justified around the origin point
	/// </summary>
	public static Rect Justified(in Vector2 origin, float width, float height, float justifyX, float justifyY)
		=> new(origin.X - (justifyX * width), origin.Y - (justifyY * height), width, height);

	/// <summary>
	/// Get a rect justified around the origin point
	/// </summary>
	public static Rect Justified(in Vector2 origin, in Vector2 size, in Vector2 justify)
		=> new(origin.X - (justify.X * size.X), origin.Y - (justify.Y * size.Y), size.X, size.Y);

	/// <summary>
	/// Get the rect with positive width and height that stretches from point a to point b
	/// </summary>
	public static Rect Between(in Vector2 a, in Vector2 b)
	{
		Rect rect;

		rect.X = a.X < b.X ? a.X : b.X;
		rect.Y = a.Y < b.Y ? a.Y : b.Y;
		rect.Width = (a.X > b.X ? a.X : b.X) - rect.X;
		rect.Height = (a.Y > b.Y ? a.Y : b.Y) - rect.Y;

		return rect;
	}

	#endregion

	/// <summary>
	/// Get the rect as a tuple of floats
	/// </summary>
	public readonly (float X, float Y, float Width, float Height) Deconstruct() => (X, Y, Width, Height);

	/// <summary>
	/// Get the rect as a tuple of floats
	/// </summary>
	public readonly void Deconstruct(out float x, out float y, out float width, out float height) => (x, y, width, height) = (X, Y, Width, Height);

	public readonly bool Equals(Rect other) => this == other;
	public readonly override bool Equals(object? obj) => (obj is Rect other) && (this == other);
	public readonly override int GetHashCode() => HashCode.Combine(X, Y, Width, Height);
	public readonly override string ToString() => $"[{X}, {Y}, {Width}, {Height}]";

	public static implicit operator Rect((float X, float Y, float Width, float Height) tuple) => new(tuple.X, tuple.Y, tuple.Width, tuple.Height);
	public static implicit operator Rect(in Vector4 vec) => new(vec.X, vec.Y, vec.Z, vec.W);
	public static implicit operator Vector4(in Rect rect) => new(rect.X, rect.Y, rect.Width, rect.Height);
	public static explicit operator RectInt(in Rect rect) => rect.Int();

	public static bool operator ==(in Rect a, in Rect b) => a.X == b.X && a.Y == b.Y && a.Width == b.Width && a.Height == b.Height;
	public static bool operator !=(in Rect a, in Rect b) => !(a == b);
	public static Rect operator +(in Rect a, in Vector2 b) => a.Translate(b);
	public static Rect operator -(in Rect a, in Vector2 b) => a.Translate(-b);
	public static Rect operator *(in Rect a, float scaler) => a.Scale(scaler);
	public static Rect operator /(in Rect a, float scaler) => new Rect(a.X / scaler, a.Y / scaler, a.Width / scaler, a.Height / scaler).ValidateSize();
	public static Rect operator *(in Rect a, int scaler) => a.Scale(scaler);
	public static Rect operator /(in Rect a, int scaler) => new Rect(a.X / scaler, a.Y / scaler, a.Width / scaler, a.Height / scaler).ValidateSize();
	public static Rect operator *(in Rect a, in Vector2 scaler) => a.Scale(scaler);
	public static Rect operator /(in Rect a, in Vector2 scaler) => new Rect(a.X / scaler.X, a.Y / scaler.Y, a.Width / scaler.X, a.Height / scaler.Y).ValidateSize();

	// TODO: remove once Facing is deleted
#pragma warning disable 0618
	public static Rect operator *(in Rect rect, Facing flipX) => flipX == Facing.Right ? rect : rect.ScaleX(-1);
#pragma warning restore 0618

	public class JsonConverter : JsonConverter<Rect>
	{
		public override Rect Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			Rect value = new();
			if (reader.TokenType != JsonTokenType.StartObject)
				return value;

			while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
			{
				if (reader.TokenType != JsonTokenType.PropertyName)
					continue;

				var component = reader.ValueSpan;
				if (!reader.Read() || reader.TokenType != JsonTokenType.Number)
				{
					reader.Skip();
					continue;
				}

				if (Calc.EqualsOrdinalIgnoreCaseUtf8(component, "x"u8))
					value.X = reader.GetSingle();
				else if (Calc.EqualsOrdinalIgnoreCaseUtf8(component, "y"u8))
					value.Y = reader.GetSingle();
				else if (Calc.EqualsOrdinalIgnoreCaseUtf8(component, "h"u8) ||
					Calc.EqualsOrdinalIgnoreCaseUtf8(component, "width"u8))
					value.Width = reader.GetSingle();
				else if (Calc.EqualsOrdinalIgnoreCaseUtf8(component, "h"u8) ||
					Calc.EqualsOrdinalIgnoreCaseUtf8(component, "height"u8))
					value.Height = reader.GetSingle();
				else
					reader.Skip();
			}

			return value;
		}

		public override void Write(Utf8JsonWriter writer, Rect value, JsonSerializerOptions options)
		{
			writer.WriteStartObject();
			writer.WriteNumber("X", value.X);
			writer.WriteNumber("Y", value.Y);
			writer.WriteNumber("Width", value.Width);
			writer.WriteNumber("Height", value.Height);
			writer.WriteEndObject();
		}
	}
}
