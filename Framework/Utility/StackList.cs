using System.Collections;

namespace Foster.Framework;

public struct StackList8<T> : IEnumerable<T>, IList<T>
{
	public const int TypeCapacity = 8;

	T v0; T v1; T v2; T v3; T v4; T v5; T v6; T v7;
	
	private int count;

	public int Count => count;
	public int Capacity => TypeCapacity;
	public bool IsReadOnly => false;

	public void Add(T value)
	{
		if (count >= Capacity)
			throw new OutOfMemoryException();
		count++;
		this[count - 1] = value;

		var test = new List<int>();
		test.IndexOf(10);
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
			this[i] = this[i + 1];
		count--;
	}

	public void Clear()
	{
		for (int i = 0; i < count; i++)
			this[i] = default(T)!;
		count = 0;
	}

	public int IndexOf(T item)
	{
		throw new NotImplementedException();
	}

	public void Insert(int index, T item)
	{
		throw new NotImplementedException();
	}

	public bool Contains(T item)
	{
		throw new NotImplementedException();
	}

	public void CopyTo(T[] array, int arrayIndex)
	{
		throw new NotImplementedException();
	}

	public bool Remove(T item)
	{
		throw new NotImplementedException();
	}

	public T this[int index]
	{
		get
		{
			if (index < count)
			{
				switch (index)
				{
					case 0: return v0; case 1: return v1; case 2: return v2; case 3: return v3; 
					case 4: return v4; case 5: return v5; case 6: return v6; case 7: return v7;
				}
			}
			throw new IndexOutOfRangeException();
		}
		set
		{
			if (index < count)
			{
				switch (index)
				{
					case 0: v0 = value; return; case 1: v1 = value; return; case 2: v2 = value; return; case 3: v3 = value; return;
					case 4: v4 = value; return; case 5: v5 = value; return; case 6: v6 = value; return; case 7: v7 = value; return;
				}
			}
			throw new IndexOutOfRangeException();
		}
	}

	public Enumerator GetEnumerator() => new(this);

	IEnumerator<T> IEnumerable<T>.GetEnumerator() => throw new NotImplementedException("Boxing Value");
	IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException("Boxing Value");

	public struct Enumerator : IEnumerator<T>
	{
		StackList8<T> list;
		int index;

		public Enumerator(in StackList8<T> list)
		{
			this.list = list;
			this.index = -1;
		}

		public T Current => list[index];
		object IEnumerator.Current => throw new NotImplementedException("Boxing Value");

		public void Dispose() { }

		public bool MoveNext()
		{
			index++;
			return index < list.Count;
		}

		public void Reset()
		{
			index = -1;
		}
	}
}

public struct StackList16<T>
{
	public const int Capacity = 16;

	T v0; T v1; T v2; T v3; T v4; T v5; T v6; T v7;
	T v8; T v9; T v10; T v11; T v12; T v13; T v14; T v15;
	
	private int count;
	public int Count => count;

	public void Add(T value)
	{
		if (count >= Capacity)
			throw new OutOfMemoryException();
		count++;
		this[count - 1] = value;
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
		for (int i = index; i < count; i ++)
			this[i] = this[i + 1];
		count--;
	}

	public T this[int index]
	{
		get
		{
			if (index < count)
			{
				switch (index)
				{
					case 0: return v0; case 1: return v1; case 2: return v2; case 3: return v3; 
					case 4: return v4; case 5: return v5; case 6: return v6; case 7: return v7;
					case 8: return v8; case 9: return v9; case 10: return v10; case 11: return v11;
					case 12: return v12; case 13: return v13; case 14: return v14; case 15: return v15;
				}
			}
			throw new IndexOutOfRangeException();
		}
		set
		{
			if (index < count)
			{
				switch (index)
				{
					case 0: v0 = value; return; case 1: v1 = value; return; case 2: v2 = value; return; case 3: v3 = value; return; 
					case 4: v4 = value; return; case 5: v5 = value; return; case 6: v6 = value; return; case 7: v7 = value; return;
					case 8: v8 = value; return; case 9: v9 = value; return; case 10: v10 = value; return; case 11: v11 = value; return;
					case 12: v12 = value; return; case 13: v13 = value; return; case 14: v14 = value; return; case 15: v15 = value; return;
				}
			}
			throw new IndexOutOfRangeException();
		}
	}
}