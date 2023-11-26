using System.Collections;
using System.Runtime.CompilerServices;

namespace Foster.Framework;

/// <summary>
/// StackList is a simple stack-allocated list, useful for holding small arrays
/// of data on the stack without allocating heap memory. They do not expand 
/// past their specified capacity.
/// TODO: each capacity could be code-generated instead of copy+pasted
/// </summary>
public struct StackList8<T> : IEnumerable<T>, IList<T>
{
	public const int TypeCapacity = 8;

	[InlineArray(TypeCapacity)]
	private struct Elements { private T _element0; }

	private Elements elements;
	private int count;

	public readonly int Count => count;
	public readonly int Capacity => TypeCapacity;
	public readonly bool IsReadOnly => false;

	public void Add(T value)
	{
		if (count >= Capacity)
			throw new OutOfMemoryException();
		elements[count++] = value;
	}

	public void Resize(int count)
	{
		if (count >= Capacity)
			throw new OutOfMemoryException();
		this.count = count;
	}

	public void RemoveAt(int index)
	{
		if (index < 0 || index >= count)
			throw new IndexOutOfRangeException();
		for (int i = index; i < count - 1; i ++)
			elements[i] = elements[i + 1];
		count--;
	}

	public void Clear()
	{
		for (int i = 0; i < count; i++)
			elements[i] = default!;
		count = 0;
	}

	public readonly int IndexOf(T item)
	{
		for (int i = 0; i < count; i ++)
			if (EqualityComparer<T>.Default.Equals(elements[i], item))
				return i;
		return -1;
	}

	public void Insert(int index, T item)
	{
		if (count >= Capacity)
			throw new OutOfMemoryException();
		for (int i = count - 1; i > index; i --)
			elements[i] = elements[i - 1];
		elements[index] = item;
	}

	public readonly bool Contains(T item)
		=> IndexOf(item) >= 0;

	public readonly void CopyTo(T[] array, int arrayIndex)
	{
		for (int i = 0; i < count; i ++)
			array[arrayIndex + i] = elements[i];
	}

	public bool Remove(T item)
	{
		var index = IndexOf(item);
		if (index >= 0)
		{
			for (int i = index; i < count - 1; i ++)
				elements[i] = elements[i + 1];
			count--;
			elements[count] = default!;
			return true;
		}
		return false;
	}

	public T this[int index]
	{
		readonly get
		{
			if (index >= count)
				throw new IndexOutOfRangeException();
			return elements[index];
		}
		set
		{
			if (index >= count)
				throw new IndexOutOfRangeException();
			elements[index] = value;
		}
	}

	public readonly Enumerator GetEnumerator() => new(this);

	readonly IEnumerator<T> IEnumerable<T>.GetEnumerator()
		=> new Enumerator(this);

	readonly IEnumerator IEnumerable.GetEnumerator()
		=> new Enumerator(this);

	public struct Enumerator(in StackList8<T> list) : IEnumerator<T>
	{
		private StackList8<T> list = list;
		private int index = -1;

		public readonly T Current => list[index];
        readonly object IEnumerator.Current => Current!;

		public readonly void Dispose() { }
        public bool MoveNext() => (++index) < list.Count;
        public void Reset() => index = -1;
    }
}

public struct StackList16<T> : IEnumerable<T>, IList<T>
{
	public const int TypeCapacity = 16;

	[InlineArray(TypeCapacity)]
	private struct Elements { private T _element0; }

	private Elements elements;
	private int count;

	public readonly int Count => count;
	public readonly int Capacity => TypeCapacity;
	public readonly bool IsReadOnly => false;

	public void Add(T value)
	{
		if (count >= Capacity)
			throw new OutOfMemoryException();
		elements[count++] = value;
	}

	public void Resize(int count)
	{
		if (count >= Capacity)
			throw new OutOfMemoryException();
		this.count = count;
	}

	public void RemoveAt(int index)
	{
		if (index < 0 || index >= count)
			throw new IndexOutOfRangeException();
		for (int i = index; i < count - 1; i ++)
			elements[i] = elements[i + 1];
		count--;
	}

	public void Clear()
	{
		for (int i = 0; i < count; i++)
			elements[i] = default!;
		count = 0;
	}

	public readonly int IndexOf(T item)
	{
		for (int i = 0; i < count; i ++)
			if (EqualityComparer<T>.Default.Equals(elements[i], item))
				return i;
		return -1;
	}

	public void Insert(int index, T item)
	{
		if (count >= Capacity)
			throw new OutOfMemoryException();
		for (int i = count - 1; i > index; i --)
			elements[i] = elements[i - 1];
		elements[index] = item;
	}

	public readonly bool Contains(T item)
		=> IndexOf(item) >= 0;

	public readonly void CopyTo(T[] array, int arrayIndex)
	{
		for (int i = 0; i < count; i ++)
			array[arrayIndex + i] = elements[i];
	}

	public bool Remove(T item)
	{
		var index = IndexOf(item);
		if (index >= 0)
		{
			for (int i = index; i < count - 1; i ++)
				elements[i] = elements[i + 1];
			count--;
			elements[count] = default!;
			return true;
		}
		return false;
	}

	public T this[int index]
	{
		readonly get
		{
			if (index >= count)
				throw new IndexOutOfRangeException();
			return elements[index];
		}
		set
		{
			if (index >= count)
				throw new IndexOutOfRangeException();
			elements[index] = value;
		}
	}

	public readonly Enumerator GetEnumerator() => new(this);

	readonly IEnumerator<T> IEnumerable<T>.GetEnumerator()
		=> new Enumerator(this);

	readonly IEnumerator IEnumerable.GetEnumerator()
		=> new Enumerator(this);

	public struct Enumerator(in StackList16<T> list) : IEnumerator<T>
	{
		private StackList16<T> list = list;
		private int index = -1;

		public readonly T Current => list[index];
        readonly object IEnumerator.Current => Current!;

		public readonly void Dispose() { }
        public bool MoveNext() => (++index) < list.Count;
        public void Reset() => index = -1;
    }
}

public struct StackList32<T> : IEnumerable<T>, IList<T>
{
	public const int TypeCapacity = 32;

	[InlineArray(TypeCapacity)]
	private struct Elements { private T _element0; }

	private Elements elements;
	private int count;

	public readonly int Count => count;
	public readonly int Capacity => TypeCapacity;
	public readonly bool IsReadOnly => false;

	public void Add(T value)
	{
		if (count >= Capacity)
			throw new OutOfMemoryException();
		elements[count++] = value;
	}

	public void Resize(int count)
	{
		if (count >= Capacity)
			throw new OutOfMemoryException();
		this.count = count;
	}

	public void RemoveAt(int index)
	{
		if (index < 0 || index >= count)
			throw new IndexOutOfRangeException();
		for (int i = index; i < count - 1; i ++)
			elements[i] = elements[i + 1];
		count--;
	}

	public void Clear()
	{
		for (int i = 0; i < count; i++)
			elements[i] = default!;
		count = 0;
	}

	public readonly int IndexOf(T item)
	{
		for (int i = 0; i < count; i ++)
			if (EqualityComparer<T>.Default.Equals(elements[i], item))
				return i;
		return -1;
	}

	public void Insert(int index, T item)
	{
		if (count >= Capacity)
			throw new OutOfMemoryException();
		for (int i = count - 1; i > index; i --)
			elements[i] = elements[i - 1];
		elements[index] = item;
	}

	public readonly bool Contains(T item)
		=> IndexOf(item) >= 0;

	public readonly void CopyTo(T[] array, int arrayIndex)
	{
		for (int i = 0; i < count; i ++)
			array[arrayIndex + i] = elements[i];
	}

	public bool Remove(T item)
	{
		var index = IndexOf(item);
		if (index >= 0)
		{
			for (int i = index; i < count - 1; i ++)
				elements[i] = elements[i + 1];
			count--;
			elements[count] = default!;
			return true;
		}
		return false;
	}

	public T this[int index]
	{
		readonly get
		{
			if (index >= count)
				throw new IndexOutOfRangeException();
			return elements[index];
		}
		set
		{
			if (index >= count)
				throw new IndexOutOfRangeException();
			elements[index] = value;
		}
	}

	public readonly Enumerator GetEnumerator() => new(this);

	readonly IEnumerator<T> IEnumerable<T>.GetEnumerator()
		=> new Enumerator(this);

	readonly IEnumerator IEnumerable.GetEnumerator()
		=> new Enumerator(this);

	public struct Enumerator(in StackList32<T> list) : IEnumerator<T>
	{
		private StackList32<T> list = list;
		private int index = -1;

		public readonly T Current => list[index];
        readonly object IEnumerator.Current => Current!;

		public readonly void Dispose() { }
        public bool MoveNext() => (++index) < list.Count;
        public void Reset() => index = -1;
    }
}

public struct StackList64<T> : IEnumerable<T>, IList<T>
{
	public const int TypeCapacity = 64;

	[InlineArray(TypeCapacity)]
	private struct Elements { private T _element0; }

	private Elements elements;
	private int count;

	public readonly int Count => count;
	public readonly int Capacity => TypeCapacity;
	public readonly bool IsReadOnly => false;

	public void Add(T value)
	{
		if (count >= Capacity)
			throw new OutOfMemoryException();
		elements[count++] = value;
	}

	public void Resize(int count)
	{
		if (count >= Capacity)
			throw new OutOfMemoryException();
		this.count = count;
	}

	public void RemoveAt(int index)
	{
		if (index < 0 || index >= count)
			throw new IndexOutOfRangeException();
		for (int i = index; i < count - 1; i ++)
			elements[i] = elements[i + 1];
		count--;
	}

	public void Clear()
	{
		for (int i = 0; i < count; i++)
			elements[i] = default!;
		count = 0;
	}

	public readonly int IndexOf(T item)
	{
		for (int i = 0; i < count; i ++)
			if (EqualityComparer<T>.Default.Equals(elements[i], item))
				return i;
		return -1;
	}

	public void Insert(int index, T item)
	{
		if (count >= Capacity)
			throw new OutOfMemoryException();
		for (int i = count - 1; i > index; i --)
			elements[i] = elements[i - 1];
		elements[index] = item;
	}

	public readonly bool Contains(T item)
		=> IndexOf(item) >= 0;

	public readonly void CopyTo(T[] array, int arrayIndex)
	{
		for (int i = 0; i < count; i ++)
			array[arrayIndex + i] = elements[i];
	}

	public bool Remove(T item)
	{
		var index = IndexOf(item);
		if (index >= 0)
		{
			for (int i = index; i < count - 1; i ++)
				elements[i] = elements[i + 1];
			count--;
			elements[count] = default!;
			return true;
		}
		return false;
	}

	public T this[int index]
	{
		readonly get
		{
			if (index >= count)
				throw new IndexOutOfRangeException();
			return elements[index];
		}
		set
		{
			if (index >= count)
				throw new IndexOutOfRangeException();
			elements[index] = value;
		}
	}

	public readonly Enumerator GetEnumerator() => new(this);

	readonly IEnumerator<T> IEnumerable<T>.GetEnumerator()
		=> new Enumerator(this);

	readonly IEnumerator IEnumerable.GetEnumerator()
		=> new Enumerator(this);

	public struct Enumerator(in StackList64<T> list) : IEnumerator<T>
	{
		private StackList64<T> list = list;
		private int index = -1;

		public readonly T Current => list[index];
        readonly object IEnumerator.Current => Current!;

		public readonly void Dispose() { }
        public bool MoveNext() => (++index) < list.Count;
        public void Reset() => index = -1;
    }
}