using System.Collections;
using System.Diagnostics;

namespace Foster.Framework;

/// <summary>
/// A class that is notified when it's recycled
/// </summary>
public interface IPoolable
{
	public void Recycle();
}

/// <summary>
/// Simple static Object pool, call <see cref="Return(T)"/> to return objects to the pool.
/// </summary>
public static class Pool<T> where T : class, new()
{
	private static readonly Queue<T> available = new();
	private static readonly object mutex = new();
	
	/// <summary>
	/// How to zero-out the object before it's used again.
	/// </summary>
	public static Action<T>? ZeroOut;

	/// <summary>
	/// Requests an instance from the Pool and returns it
	/// </summary>
	public static T Get()
	{
		T instance;
		lock(mutex)
			instance = (available.Count > 0 ? available.Dequeue() : new T());
		return instance;
	}

	/// <summary>
	/// Returns an instance to the Pool so it can be recycled for later.
	/// </summary>
	public static void Return(T instance)
	{
		Debug.Assert(instance != null, "Can't return a null instance");
		if (instance is IList list)
			list.Clear();
		else if (instance is IPoolable returnable)
			returnable.Recycle();
		ZeroOut?.Invoke(instance);
		lock(mutex)
			available.Enqueue(instance);
	}

	public static void Clear()
	{
		lock(mutex)
			available.Clear();
	}
}

/// <summary>
/// Shorthand to Generic Pool because it's nicer to write this way imo
/// </summary>
public static class Pool
{
	public static T Get<T>() where T : class, new()
	{
		return Pool<T>.Get();
	}

	public static void Return<T>(T instance) where T : class, new()
	{
		Pool<T>.Return(instance);
	}
}

/// <summary>
/// Objects retrieved from this pool are returned to the pool at the start of the next frame.
/// </summary>
public static class FramePool<T> where T : class, new()
{
	private static readonly Queue<T> available = new();
	private static readonly List<T> usedThisFrame = new();
	private static readonly object mutex = new();

	static FramePool()
	{
		FramePool.nextFrame += NextFrame;
	}

	/// <summary>
	/// Gets an instance from the Pool, or creates one if it doesn't exit
	/// </summary>
	public static T Get()
	{
		T instance;

		lock(mutex)
		{
			instance = (available.Count > 0 ? available.Dequeue() : new T());
			usedThisFrame.Add(instance);
		}

		return instance;
	}

	/// <summary>
	/// Clears all objects from the Pool, allowing them to be collected.
	/// </summary>
	public static void Clear()
	{
		lock(mutex)
		{
			available.Clear();
			usedThisFrame.Clear();
		}
	}

	/// <summary>
	/// Steps the Pool to the next frame, recycling all objects.
	/// </summary>
	private static void NextFrame()
	{
		lock(mutex)
		{
			foreach (var it in usedThisFrame)
			{
				if (it is IList list)
					list.Clear();
				if (it is IPoolable returnable)
					returnable.Recycle();
				Pool<T>.ZeroOut?.Invoke(it);
				available.Enqueue(it);
			}
			usedThisFrame.Clear();
		}
	}
}

public static class FramePool
{
	/// <summary>
	/// Action to perform when stepping to the next frame.
	/// <see cref="FramePool{T}"/> registers its NextFrame function with this event.
	/// </summary>
	internal static event Action? nextFrame;

	/// <summary>
	/// Shorthand to Generic <see cref="FramePool{T}.Get"/>.
	/// </summary>
	public static T Get<T>() where T : class, new()
	{
		return FramePool<T>.Get();
	}

	/// <summary>
	/// Steps the Frame Pools to the next frame
	/// </summary>
	internal static void NextFrame()
	{
		nextFrame?.Invoke();
	}
}
