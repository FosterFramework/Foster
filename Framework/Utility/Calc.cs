using System.Buffers.Text;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace Foster.Framework;

/// <summary>
/// Utility Functions
/// </summary>
public static class Calc
{
	#region Consts

	/// <summary>
	/// PI in radians
	/// </summary>
	public const float PI = MathF.PI;

	/// <summary>
	/// Half PI in radians
	/// </summary>
	public const float HalfPI = MathF.PI / 2f;

	/// <summary>
	/// TAU (2-PI) in radians
	/// </summary>
	public const float TAU = MathF.PI * 2f;

	/// <summary>
	/// Converts Degrees to Radians
	/// </summary>
	public const float DegToRad = (MathF.PI * 2) / 360f;

	/// <summary>
	/// Converts Radians to Degrees
	/// </summary>
	public const float RadToDeg = 360f / (MathF.PI * 2);

	public const float Right = 0;
	public const float Left = PI;
	public const float Up = PI + HalfPI;
	public const float Down = HalfPI;
	public const float UpRight = TAU - PI * 0.25f;
	public const float DownRight = PI * 0.25f;
	public const float UpLeft = TAU - PI * 0.75f;
	public const float DownLeft = PI * 0.75f;

	#endregion

	#region Enums

	public static int EnumCount<T>() where T : struct, Enum
		=> Enum.GetValues<T>().Length;

	/// <summary>
	/// Performantly convert an enum to int
	/// </summary>
	public static unsafe int EnumAsInt<TEnum>(TEnum enumValue) where TEnum : unmanaged, Enum
		=> *(int*)(&enumValue);

	#endregion

	#region Binary  Operations

	public static bool IsBitSet(byte b, int pos)
		=> (b & (1 << pos)) != 0;

	public static bool IsBitSet(int b, int pos)
		=> (b & (1 << pos)) != 0;

	#endregion

	#region Give Me

	public static T GiveMe<T>(int index, params ReadOnlySpan<T> choices)
		=> choices[index];

	#endregion

	#region Math

	public static bool HasAllFlags(this uint flags, uint check)
		=> (flags & check) == check;

	public static bool HasAnyFlags(this uint flags, uint check)
		=> (flags & check) != 0;

	public static bool HasAllFlags(this ulong flags, ulong check)
		=> (flags & check) == check;

	public static bool HasAnyFlags(this ulong flags, ulong check)
		=> (flags & check) != 0;

	public static float Avg(float a, float b)
		=> a + (b - a) * .5f;

	public static bool SignsMatch(float a, float b)
		=> Math.Sign(a) == Math.Sign(b);

	public static float Squared(this float v)
		=> v * v;

	/// <summary>
	/// Get the area of a triangle
	/// </summary>
	public static float TriangleArea(in Vector2 triA, in Vector2 triB, in Vector2 triC)
		=> MathF.Abs((triA.X * (triB.Y - triC.Y)
					+ triB.X * (triC.Y - triA.Y)
					+ triC.X * (triA.Y - triB.Y)) * .5f);

	/// <summary>
	/// Get the cross product of two Vector2s, ie. (a.X * b.Y) - (a.Y * b.X)
	/// </summary>
	public static float Cross(in Vector2 a, in Vector2 b)
		=> (a.X * b.Y) - (a.Y * b.X);

	/// <summary>
	/// Get the integral sign of the cross product of two Vector2s
	/// </summary>
	public static int SignCross(in Vector2 a, in Vector2 b)
		=> MathF.Sign(Cross(a, b));

	/// <summary>
	/// Get whether the sequence of points takes a right- or left-hand turn (-1 or 1 respectively, or 0 for no turn)
	/// </summary>
	public static int Orient(in Vector2 pA, in Vector2 pB, in Vector2 pC)
		=> SignCross(new(pB.X - pA.X, pB.Y - pA.Y), new(pC.X - pA.X, pC.Y - pA.Y));

	/// <summary>
	/// Gets whether the triangle contains the point
	/// </summary>
	public static bool TriangleContainsPoint(in Vector2 triA, in Vector2 triB, in Vector2 triC, in Vector2 point)
		=> Math.Abs(Orient(triA, triB, point)
			+ Orient(triB, triC, point)
			+ Orient(triC, triA, point)) == 3;

	/// <summary>
	/// Shorthand for the absolute value of the dot product of two <seealso cref="Vector2"/>s
	/// </summary>
	public static float AbsDot(Vector2 a, Vector2 b)
		=> MathF.Abs(Vector2.Dot(a, b));

	/// <summary>
	/// Shorthand for the square of the dot product of two <seealso cref="Vector2"/>s, but preserving the sign of the dot product. When you square a dot product, it normalizes it so that, for example, 0.5 = a 45 degree angle difference
	/// </summary>
	public static float DotSq(Vector2 a, Vector2 b)
	{
		float dot = Vector2.Dot(a, b);
		return MathF.Sign(dot) * dot * dot;
	}

	/// <summary>
	/// Shorthand for the square of the dot product of two <seealso cref="Vector2"/>s. The sign of the dot product is not preserved, so this will always be positive.
	/// </summary>
	public static float AbsDotSq(Vector2 a, Vector2 b)
		=> Squared(Vector2.Dot(a, b));

	public static T Min<T>(T a, T b) where T : IComparable<T>
		=> a.CompareTo(b) < 0 ? a : b;

	public static T Min<T>(T a, T b, T c) where T : IComparable<T>
		=> Min(Min(a, b), c);

	public static T Min<T>(T a, T b, T c, T d) where T : IComparable<T>
		=> Min(Min(Min(a, b), c), d);

	public static T Max<T>(T a, T b) where T : IComparable<T>
		=> a.CompareTo(b) > 0 ? a : b;

	public static T Max<T>(T a, T b, T c) where T : IComparable<T>
		=> Max(Max(a, b), c);

	public static T Max<T>(T a, T b, T c, T d) where T : IComparable<T>
		=> Max(Max(Max(a, b), c), d);

	/// <summary>
	/// Returns a vector whose X and Y are the minimums of the three Xs and Ys of the given vectors
	/// </summary>
	public static Vector2 Min(Vector2 a, Vector2 b, Vector2 c)
		=> Vector2.Min(Vector2.Min(a, b), c);

	/// <summary>
	/// Returns a vector whose X and Y are the maximums of the three Xs and Ys of the given vectors
	/// </summary>
	public static Vector2 Max(Vector2 a, Vector2 b, Vector2 c)
		=> Vector2.Max(Vector2.Max(a, b), c);

	/// <summary>
	/// Get the index of the element in the list that is smallest. If multiple entries are equal, the one that appears first is chosen. Returns -1 if the list is empty.
	/// </summary>
	public static int Smallest<T>(params ReadOnlySpan<T> list) where T : IComparable<T>
	{
		if (list.Length == 0)
			return -1;

		int index = 0;
		T val = list[0];

		for (int i = 1; i < list.Length; i++)
			if (list[i].CompareTo(val) < 0)
			{
				index = i;
				val = list[i];
			}

		return index;
	}

	/// <summary>
	/// Get the index of the element in the list that is largest. If multiple entries are equal, the one that appears first is chosen. Returns -1 if the list is empty.
	/// </summary>
	public static int Largest<T>(params ReadOnlySpan<T> list) where T : IComparable<T>
	{
		if (list.Length == 0)
			return -1;

		int index = 0;
		T val = list[0];

		for (int i = 1; i < list.Length; i++)
			if (list[i].CompareTo(val) < 0)
			{
				index = i;
				val = list[i];
			}

		return index;
	}

	/// <summary>
	/// Move toward a target value without passing it
	/// </summary>
	public static float Approach(float from, float target, float amount)
	{
		if (from > target)
			return Math.Max(from - amount, target);
		else
			return Math.Min(from + amount, target);
	}

	/// <summary>
	/// Move toward a target value without passing it
	/// </summary>
	public static float Approach(ref float from, float target, float amount)
	{
		if (from > target)
			return from = Math.Max(from - amount, target);
		else
			return from = Math.Min(from + amount, target);
	}

	/// <summary>
	/// Move toward a target value without passing it, and only if we have the opposite sign or lower magnitude
	/// </summary>
	public static float ApproachIfLower(float from, float target, float amount)
	{
		if (Math.Sign(from) != Math.Sign(target) || Math.Abs(from) < Math.Abs(target))
			return Approach(from, target, amount);
		else
			return from;
	}

	/// <summary>
	/// Move toward a target value without passing it, and only if we have the opposite sign or lower magnitude
	/// </summary>
	public static float ApproachIfLower(ref float from, float target, float amount)
	{
		if (Math.Sign(from) != Math.Sign(target) || Math.Abs(from) < Math.Abs(target))
			return Approach(ref from, target, amount);
		else
			return from;
	}

	public static Vector2 Approach(Vector2 from, Vector2 target, float amount)
	{
		if (from == target)
			return target;
		else
		{
			var diff = target - from;
			if (diff.LengthSquared() <= amount * amount)
				return target;
			else
				return from + diff.Normalized() * amount;
		}
	}

	public static Vector2 Approach(ref Vector2 from, Vector2 target, float amount)
	{
		if (from == target)
			return target;
		else
		{
			var diff = target - from;
			if (diff.LengthSquared() <= amount * amount)
				return from = target;
			else
				return from += diff.Normalized() * amount;
		}
	}

	public static Vector2 RotateToward(Vector2 dir, Vector2 target, float maxAngleDelta, float maxMagnitudeDelta)
	{
		float angle = dir.Angle();
		float len = dir.Length();

		if (maxAngleDelta > 0f)
			angle = AngleApproach(angle, target.Angle(), maxAngleDelta);

		if (maxMagnitudeDelta > 0f)
			len = Approach(len, target.Length(), maxMagnitudeDelta);

		return AngleToVector(angle, len);
	}

	/// <summary>
	/// Clamps a number between two values
	/// </summary>
	public static int Clamp(int value, int min, int max)
	{
		return Math.Min(Math.Max(value, min), max);
	}

	/// <summary>
	/// Clamps a number between two values
	/// </summary>
	public static float Clamp(float value, float min, float max)
	{
		return Math.Min(Math.Max(value, min), max);
	}

	/// <summary>
	/// Clamps a number between 0 and 1
	/// </summary>
	public static float Clamp(float value)
	{
		return Math.Min(Math.Max(value, 0), 1);
	}

	/// <summary>
	/// Shorthand to MathF.Round but returns an Integer
	/// </summary>
	public static int Round(float v) => (int)MathF.Round(v);

	/// <summary>
	/// Shorthand to MathF.Floor but returns an Integer
	/// </summary>
	public static int Floor(float v) => (int)MathF.Floor(v);

	/// <summary>
	/// Shorthand to MathF.Ceiling but returns an Integer
	/// </summary>
	public static int Ceil(float v) => (int)MathF.Ceiling(v);

	/// <summary>
	/// Converts a value from 0 to 1, to 0 to 1 to 0
	/// </summary>
	public static float YoYo(float value)
	{
		if (value <= .5f)
			return value * 2;
		else
			return 1 - ((value - .5f) * 2);
	}

	/// <summary>
	/// Remaps a value from min-max, to newMin-newMax
	/// </summary>
	public static float Map(float val, float min, float max, float newMin = 0, float newMax = 1)
		=> ((val - min) / (max - min)) * (newMax - newMin) + newMin;

	/// <summary>
	/// Remaps a value from min-max, to newMin-newMax, but clamps the value within the given range
	/// </summary>
	public static float ClampedMap(float val, float min, float max, float newMin = 0, float newMax = 1)
		=> Clamp((val - min) / (max - min), 0, 1) * (newMax - newMin) + newMin;

	/// <summary>
	/// Remaps the given Sin(radians) value
	/// </summary>
	public static float SineMap(float radians, float newMin, float newMax)
		=> Map(MathF.Sin(radians), -1, 1, newMin, newMax);

	public static float Angle(Vector2 vec)
		=> MathF.Atan2(vec.Y, vec.X);

	public static float Angle(Vector2 from, Vector2 to)
		=> MathF.Atan2(to.Y - from.Y, to.X - from.X);

	public static Vector2 AngleToVector(float angle, float length = 1)
		=> new (MathF.Cos(angle) * length, MathF.Sin(angle) * length);

	public static float AngleApproach(float val, float target, float maxMove)
	{
		var diff = AngleDiff(val, target);
		if (Math.Abs(diff) < maxMove)
			return target;
		return val + Clamp(diff, -maxMove, maxMove);
	}

	public static float AngleLerp(float startAngle, float endAngle, float percent)
		=> startAngle + AngleDiff(startAngle, endAngle) * percent;

	public static float AngleDiff(float radiansA, float radiansB)
		=> ((radiansB - radiansA - PI) % TAU + TAU) % TAU - PI;

	public static float AbsAngleDiff(float radiansA, float radiansB)
		=> MathF.Abs(AngleDiff(radiansA, radiansB));

	public static float AngleWrap(float radians)
		=> (radians + TAU) % TAU;

	public static float AngleReflectOnX(float radians)
		=> AngleWrap(-radians);

	public static float AngleReflectOnY(float radians)
		=> AngleWrap(HalfPI - (radians - HalfPI));

	public static bool OnInterval(float value, float prevValue, float interval, float offset = 0)
	{
		var last = ((prevValue - offset) / interval);
		var next = ((value - offset) / interval);
		return last != next;
	}

	public static int NextPowerOfTwo(int x)
	{
		x--;
		x |= x >> 1;
		x |= x >> 2;
		x |= x >> 4;
		x |= x >> 8;
		x |= x >> 16;
		x++;
		return x;
	}

	// TODO: should this use float.Epsilon?
	public static bool Approx(float a, float b)
		=> MathF.Abs(a - b) <= 0.001f;

	public static IReadOnlyList<Point2> GetBresenhamsLine(Point2 a, Point2 b)
	{
		var list = FramePool<List<Point2>>.Get();

		bool steep = Math.Abs(b.Y - a.Y) > Math.Abs(b.X - a.X);
		if (steep)
		{
			Swap(ref a.X, ref a.Y);
			Swap(ref b.X, ref b.Y);
		}
		if (a.X > b.X)
		{
			Swap(ref a.X, ref b.X);
			Swap(ref a.Y, ref b.Y);
		}
		int dx = b.X - a.X;
		int dy = Math.Abs(b.Y - a.Y);
		int error = dx / 2;
		int ystep = (a.Y < b.Y) ? 1 : -1;
		int y = a.Y;

		for (int x = a.X; x <= b.X; x++)
		{
			list.Add(new(steep ? y : x, steep ? x : y));
			error -= dy;
			if (error < 0)
			{
				y += ystep;
				error += dx;
			}
		}

		return list;
	}

	/// <summary>
	/// Find the closest point in the list to a given point
	/// </summary>
	/// <returns>The index of the closest point, or -1 if the list is empty</returns>
	public static int GetClosestPointIndex(this IList<Vector2> points, in Vector2 to)
	{
		int closestIndex = -1;
		float closestDistSq = 0;

		for (int i = 0; i < points.Count; i++)
		{
			var distSq = Vector2.DistanceSquared(points[i], to);
			if (closestIndex == -1 || distSq < closestDistSq)
			{
				closestIndex = i;
				closestDistSq = distSq;
			}
		}

		return closestIndex;
	}

	/// <summary>
	/// Find the closest point in the list to a given point
	/// </summary>
	/// <returns>The index of the closest point, or -1 if the list is empty</returns>
	public static int GetClosestPointIndex(this IList<Point2> points, in Vector2 to)
	{
		int closestIndex = -1;
		float closestDistSq = 0;

		for (int i = 0; i < points.Count; i++)
		{
			var distSq = Vector2.DistanceSquared(points[i], to);
			if (closestIndex == -1 || distSq < closestDistSq)
			{
				closestIndex = i;
				closestDistSq = distSq;
			}
		}

		return closestIndex;
	}

	/// <summary>
	/// Check if our magnitude is smaller than Vector b's
	/// </summary>
	public static bool IsSmallerThan(this Vector2 a, in Vector2 b)
		=> a.LengthSquared() < b.LengthSquared();

	#endregion

	#region Triangulation

	public static void Triangulate(IList<Vector2> points, List<int> populate)
	{
		float Area()
		{
			var area = 0f;

			for (int p = points.Count - 1, q = 0; q < points.Count; p = q++)
			{
				var pval = points[p];
				var qval = points[q];

				area += pval.X * qval.Y - qval.X * pval.Y;
			}

			return area * 0.5f;
		}

		bool Snip(int u, int v, int w, int n, Span<int> list)
		{
			var a = points[list[u]];
			var b = points[list[v]];
			var c = points[list[w]];

			if (float.Epsilon > (((b.X - a.X) * (c.Y - a.Y)) - ((b.Y - a.Y) * (c.X - a.X))))
				return false;

			for (int p = 0; p < n; p++)
			{
				if ((p == u) || (p == v) || (p == w))
					continue;

				if (InsideTriangle(a, b, c, points[list[p]]))
					return false;
			}

			return true;
		}

		if (points.Count < 3)
			return;

		Span<int> list = points.Count < 1000
			? stackalloc int[points.Count]
			: new int[points.Count];

		if (Area() > 0)
		{
			for (int v = 0; v < points.Count; v++)
				list[v] = v;
		}
		else
		{
			for (int v = 0; v < points.Count; v++)
				list[v] = (points.Count - 1) - v;
		}

		var nv = points.Count;
		var count = 2 * nv;

		for (int v = nv - 1; nv > 2;)
		{
			if ((count--) <= 0)
				return;

			var u = v;
			if (nv <= u)
				u = 0;
			v = u + 1;
			if (nv <= v)
				v = 0;
			var w = v + 1;
			if (nv <= w)
				w = 0;

			if (Snip(u, v, w, nv, list))
			{
				populate.Add(list[u]);
				populate.Add(list[v]);
				populate.Add(list[w]);

				for (int s = v, t = v + 1; t < nv; s++, t++)
					list[s] = list[t];

				nv--;
				count = 2 * nv;
			}
		}
	}

	public static List<int> Triangulate(IList<Vector2> points)
	{
		var indices = new List<int>();
		Triangulate(points, indices);
		return indices;
	}

	public static bool InsideTriangle(Vector2 a, Vector2 b, Vector2 c, Vector2 point)
	{
		var p0 = c - b;
		var p1 = a - c;
		var p2 = b - a;

		var ap = point - a;
		var bp = point - b;
		var cp = point - c;

		return (p0.X * bp.Y - p0.Y * bp.X >= 0.0f) &&
			   (p2.X * ap.Y - p2.Y * ap.X >= 0.0f) &&
			   (p1.X * cp.Y - p1.Y * cp.X >= 0.0f);
	}

	#endregion

	#region Parsing

	public static bool ParseVector2(ReadOnlySpan<char> span, char delimiter, out Vector2 vector)
	{
		vector = Vector2.Zero;

		var index = span.IndexOf(delimiter);
		if (index >= 0)
		{
			var x = span.Slice(0, index);
			var y = span.Slice(index + 1);

			if (float.TryParse(x, NumberStyles.Float, CultureInfo.InvariantCulture, out vector.X) &&
				float.TryParse(y, NumberStyles.Float, CultureInfo.InvariantCulture, out vector.Y))
				return true;
		}

		return false;
	}

	public static bool ParseVector3(ReadOnlySpan<char> span, char deliminator, out Vector3 vector)
	{
		vector = Vector3.Zero;

		var index = span.IndexOf(deliminator);
		if (index > 0)
		{
			var first = span.Slice(0, index);
			var remaining = span.Slice(index + 1);

			index = remaining.IndexOf(deliminator);
			if (index > 0)
			{
				var second = remaining.Slice(0, index);
				var third = remaining.Slice(index + 1);

				if (float.TryParse(first, NumberStyles.Float, CultureInfo.InvariantCulture, out vector.X) &&
					float.TryParse(second, NumberStyles.Float, CultureInfo.InvariantCulture, out vector.Y) &&
					float.TryParse(third, NumberStyles.Float, CultureInfo.InvariantCulture, out vector.Z))
					return true;
			}
		}

		return false;
	}

	#endregion

	#region Utils

	/// <summary>
	/// .NET Core doesn't always hash string values the same (it can seed it based on the running instance)
	/// So this is to get a static value for every same string
	/// </summary>
	public static int StaticStringHash(ReadOnlySpan<char> value)
	{
		unchecked
		{
			int hash = 5381;
			for (int i = 0; i < value.Length; i++)
				hash = ((hash << 5) + hash) + value[i];
			return hash;
		}
	}

	public static int StaticStringHash(ReadOnlySpan<byte> value)
	{
		unchecked
		{
			int hash = 5381;
			for (int i = 0; i < value.Length; i++)
				hash = ((hash << 5) + hash) + value[i];
			return hash;
		}
	}

	/// <summary>
	/// Check if two UTF8 strings are equal, ingoring case
	/// TODO: is there a built in C# way to do this?? this seems bad
	/// </summary>
	public static bool EqualsOrdinalIgnoreCaseUtf8(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
	{
		if (a.Length != b.Length)
			return false;

		var charCountA = Encoding.UTF8.GetCharCount(a);
		var charCountB = Encoding.UTF8.GetCharCount(b);

		if (charCountA != charCountB)
			return false;

		Span<char> charsA = stackalloc char[charCountA];
		Span<char> charsB = stackalloc char[charCountB];
		Encoding.UTF8.GetChars(a, charsA);
		Encoding.UTF8.GetChars(b, charsB);

		return MemoryExtensions.Equals(charsA, charsB, StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>
	/// Returns the amount of characters the strings have in common starting from the start
	/// </summary>
	public static int AmountInCommon(this string a, string b)
	{
		int i = 0;
		for (; i < a.Length && i < b.Length; i++)
			if (a[i] != b[i])
				break;
		return i;
	}

	public static string NormalizePath(string a, string b)
	{
		return NormalizePath(Path.Join(a, b));
	}

	public static string NormalizePath(string a, string b, string c)
	{
		return NormalizePath(Path.Join(a, b, c));
	}

	public static string NormalizePath(string path)
	{
		unsafe
		{
			Span<char> temp = stackalloc char[path.Length];
			for (int i = 0; i < path.Length; i++)
				temp[i] = path[i];
			return NormalizePath(temp).ToString();
		}
	}

	public static Span<char> NormalizePath(Span<char> path)
	{
		for (int i = 0; i < path.Length; i++)
			if (path[i] == '\\') path[i] = '/';

		int length = path.Length;
		for (int i = 1, t = 1, l = length; t < l; i++, t++)
		{
			if (path[t - 1] == '/' && path[t] == '/')
			{
				i--;
				length--;
			}
			else
				path[i] = path[t];
		}

		return path[..length];
	}

	public static ReadOnlySpan<byte> ToBytes<T>(Span<T> span) where T : struct
	{
		return MemoryMarshal.Cast<T, byte>(span);
	}

	public static bool TryFirst<T>(this List<T> list, Func<T, bool> predicate, [NotNullWhen(true)] out T? match) where T : class
	{
		foreach (var t in list)
			if (predicate(t))
			{
				match = t;
				return true;
			}

		match = null;
		return false;
	}

	public static void Swap<T>(ref T a, ref T b)
		=> (b, a) = (a, b);

	#endregion

	#region Reflection

	public static bool HasAttr<T>(this MemberInfo member) where T : Attribute
		=> member.GetCustomAttribute<T>() != null;

	public static bool TryGetAttr<T>(this FieldInfo field, [NotNullWhen(true)] out T? attr) where T : Attribute
		=> (attr = field.GetCustomAttribute<T>()) != null;

	public static bool IsNullable(this PropertyInfo property) =>
		IsNullableHelper(property.PropertyType, property.DeclaringType, property.CustomAttributes);

	public static bool IsNullable(this FieldInfo field) =>
		IsNullableHelper(field.FieldType, field.DeclaringType, field.CustomAttributes);

	private static bool IsNullableHelper(Type memberType, MemberInfo? declaringType, IEnumerable<CustomAttributeData> customAttributes)
	{
		if (memberType.IsValueType)
			return Nullable.GetUnderlyingType(memberType) != null;

		var nullable = customAttributes
			.FirstOrDefault(x => x.AttributeType.FullName == "System.Runtime.CompilerServices.NullableAttribute");
		if (nullable != null && nullable.ConstructorArguments.Count == 1)
		{
			var attributeArgument = nullable.ConstructorArguments[0];
			if (attributeArgument.ArgumentType == typeof(byte[]))
			{
				var args = (ReadOnlyCollection<CustomAttributeTypedArgument>)attributeArgument.Value!;
				if (args.Count > 0 && args[0].ArgumentType == typeof(byte))
				{
					return (byte)args[0].Value! == 2;
				}
			}
			else if (attributeArgument.ArgumentType == typeof(byte))
			{
				return (byte)attributeArgument.Value! == 2;
			}
		}

		for (var type = declaringType; type != null; type = type.DeclaringType)
		{
			var context = type.CustomAttributes
				.FirstOrDefault(x => x.AttributeType.FullName == "System.Runtime.CompilerServices.NullableContextAttribute");
			if (context != null &&
				context.ConstructorArguments.Count == 1 &&
				context.ConstructorArguments[0].ArgumentType == typeof(byte))
			{
				return (byte)context.ConstructorArguments[0].Value! == 2;
			}
		}

		// Couldn't find a suitable attribute
		return false;
	}
	#endregion

	#region Interpolation

	public static float Lerp(float a, float b, float percent)
		=> (a + (b - a) * percent);

	public static float Bezier(float a, float b, float c, float t)
		=> Lerp(Lerp(a, b, t), Lerp(b, c, t), t);

	public static float Bezier(float a, float b, float c, float d, float t)
		=> Bezier(Lerp(a, b, t), Lerp(b, c, t), Lerp(c, d, t), t);

	public static Vector2 Bezier(Vector2 a, Vector2 b, Vector2 c, float t)
		=> Vector2.Lerp(Vector2.Lerp(a, b, t), Vector2.Lerp(b, c, t), t);

	public static Vector2 Bezier(Vector2 a, Vector2 b, Vector2 c, Vector2 d, float t)
		=> Bezier(Vector2.Lerp(a, b, t), Vector2.Lerp(b, c, t), Vector2.Lerp(c, d, t), t);

	public static float SmoothDamp(float current, float target, ref float velocity, float smoothTime, float maxSpeed, float deltaTime)
	{
		smoothTime = Math.Max(0.0001f, smoothTime);
		float omega = 2f / smoothTime;
		float x = omega * deltaTime;
		float exp = 1f / (1f + x + 0.48f * x * x + 0.235f * x * x * x);
		float change = current - target;
		float origTo = target;
		float maxChange = maxSpeed * smoothTime;
		change = Math.Clamp(change, -maxChange, maxChange);
		target = current - change;
		float temp = (velocity + omega * change) * deltaTime;
		velocity = (velocity - omega * temp) * exp;
		float output = target + (change + temp) * exp;
		if (origTo - current > 0f == output > origTo)
		{
			output = origTo;
			velocity = (output - origTo) / deltaTime;
		}
		return output;
	}

	#endregion

	#region Snap

	/// <summary>
	/// Round the value to the nearest interval. Eg: Snap(2.2, 1.2) = 2.4
	/// </summary>
	public static float Snap(float value, float snapTo)
		=> MathF.Round(value / snapTo) * snapTo;

	/// <summary>
	/// Round the value to the nearest interval. Eg: Snap(3.2, 2) = 4
	/// </summary>
	public static int Snap(float value, int snapTo)
		=> Round(value / snapTo) * snapTo;

	/// <summary>
	/// Round the vector to the nearest intervals. Eg: Snap([0.8, 1.9], [1.2, 1.2]) = [1.2, 2.4]
	/// </summary>
	public static Vector2 Snap(Vector2 value, Vector2 snapTo)
		=> new(Snap(value.X, snapTo.X), Snap(value.Y, snapTo.Y));

	/// <summary>
	/// Round the vector to the nearest intervals. Eg: Snap([0.8, 1.6], [1, 1]) = [1, 2]
	/// </summary>
	public static Point2 Snap(Vector2 value, Point2 snapTo)
		=> new(Snap(value.X, snapTo.X), Snap(value.Y, snapTo.Y));

	/// <summary>
	/// Round the vector to the nearest interval on both axes. Eg: Snap([0.8, 1.9], 1.2) = [1.2, 2.4]
	/// </summary>
	public static Vector2 Snap(Vector2 value, float snapTo)
		=> new(Snap(value.X, snapTo), Snap(value.Y, snapTo));

	/// <summary>
	/// Round the vector to the nearest interval on both axes. Eg: Snap([0.8, 1.6], 1) = [1, 2]
	/// </summary>
	public static Point2 Snap(Vector2 value, int snapTo)
		=> new(Snap(value.X, snapTo), Snap(value.Y, snapTo));

	/// <summary>
	/// Floor the value to the nearest interval. Eg: SnapFloor(2.2, 1.2) = 1.2
	/// </summary>
	public static float SnapFloor(float value, float snapTo)
		=> MathF.Floor(value / snapTo) * snapTo;

	/// <summary>
	/// Floor the value to the nearest interval. Eg: SnapFloor(3.2, 2) = 2
	/// </summary>
	public static int SnapFloor(float value, int snapTo)
		=> Floor(value / snapTo) * snapTo;

	/// <summary>
	/// Floor the vector to the nearest intervals. Eg: SnapFloor([0.8, 2.3], [0, 1.2]) = [1.2, 1.2]
	/// </summary>
	public static Vector2 SnapFloor(Vector2 value, Vector2 snapTo)
		=> new(SnapFloor(value.X, snapTo.X), SnapFloor(value.Y, snapTo.Y));

	/// <summary>
	/// Floor the vector to the nearest intervals. Eg: SnapFloor([0.8, 1.6], [1, 1]) = [0, 1]
	/// </summary>
	public static Point2 SnapFloor(Vector2 value, Point2 snapTo)
		=> new(SnapFloor(value.X, snapTo.X), SnapFloor(value.Y, snapTo.Y));

	/// <summary>
	/// Floor the vector to the nearest interval on both axes. Eg: SnapFloor([0.8, 2.3], 1.2) = [0, 1.2]
	/// </summary>
	public static Vector2 SnapFloor(Vector2 value, float snapTo)
		=> new(SnapFloor(value.X, snapTo), SnapFloor(value.Y, snapTo));

	/// <summary>
	/// Floor the vector to the nearest interval on both axes. Eg: SnapFloor([0.8, 1.6], 1) = [0, 1]
	/// </summary>
	public static Point2 SnapFloor(Vector2 value, int snapTo)
		=> new(SnapFloor(value.X, snapTo), SnapFloor(value.Y, snapTo));

	/// <summary>
	/// Ceil the value to the nearest interval. Eg: SnapCeil(1.4, 1.2) = 2.4
	/// </summary>
	public static float SnapCeil(float value, float snapTo)
		=> MathF.Ceiling(value / snapTo) * snapTo;

	/// <summary>
	/// Ceil the value to the nearest interval. Eg: SnapCeil(2.2, 2) = 4
	/// </summary>
	public static int SnapCeil(float value, int snapTo)
		=> Ceil(value / snapTo) * snapTo;

	/// <summary>
	/// Ceil the vector to the nearest intervals. Eg: SnapCeil([0.8, 1.3], [0, 1.2]) = [1.2, 2.4]
	/// </summary>
	public static Vector2 SnapCeil(Vector2 value, Vector2 snapTo)
		=> new(SnapCeil(value.X, snapTo.X), SnapCeil(value.Y, snapTo.Y));

	/// <summary>
	/// Ceil the vector to the nearest intervals. Eg: SnapCeil([0.8, 1.2], [1, 1]) = [1, 2]
	/// </summary>
	public static Point2 SnapCeil(Vector2 value, Point2 snapTo)
		=> new(SnapCeil(value.X, snapTo.X), SnapCeil(value.Y, snapTo.Y));

	/// <summary>
	/// Ceil the vector to the nearest interval on both axes. Eg: SnapCeil([0.8, 1.3], 1.2) = [1.2, 2.4]
	/// </summary>
	public static Vector2 SnapCeil(Vector2 value, float snapTo)
		=> new(SnapCeil(value.X, snapTo), SnapCeil(value.Y, snapTo));

	/// <summary>
	/// Ceil the vector to the nearest interval on both axes. Eg: SnapCeil([0.8, 1.2], 1) = [1, 2]
	/// </summary>
	public static Point2 SnapCeil(Vector2 value, int snapTo)
		=> new(SnapCeil(value.X, snapTo), SnapCeil(value.Y, snapTo));

	#endregion

	#region Embedded Streams

	/// <summary>
	/// Returns a byte array for an embedded file.
	/// Throws an exception if the file does not exist.
	/// </summary>
	public static byte[] ReadEmbeddedBytes(Assembly assembly, string name)
	{
		using var stream = assembly.GetManifestResourceStream(name);
		if (stream != null)
		{
			var result = new byte[stream.Length];
			stream.ReadExactly(result);
			return result;
		}

		throw new Exception($"Missing Embedded file '{name}' in {assembly}");
	}

	/// <summary>
	/// Returns a byte array for an embedded file in the calling assembly.
	/// Throws an exception if the file does not exist.
	/// </summary>
	public static byte[] ReadEmbeddedBytes(string name)
	{
		return ReadEmbeddedBytes(Assembly.GetCallingAssembly(), name);
	}

	#endregion

}
