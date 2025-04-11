using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using DAM = System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute;
using DAMT = System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes;

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
public static class Pool<[DAM(DAMT.PublicMethods)] T> where T : class, new()
{
	private static readonly ConcurrentQueue<T> available = new();

	/// <summary>
	/// How to zero-out the object before it's used again.
	/// </summary>
	public static Action<T> ZeroOut { get; set; } = CreateDefaultClear();

	/// <summary>
	/// Requests an instance from the Pool and returns it
	/// </summary>
	public static T Get()
	{
		if (!available.TryDequeue(out var instance))
			instance = new();
		return instance;
	}

	/// <summary>
	/// Returns an instance to the Pool so it can be recycled for later.
	/// </summary>
	public static void Return(T instance)
	{
		Debug.Assert(instance != null, "Can't return a null instance");
		ZeroOut.Invoke(instance);
		available.Enqueue(instance);
	}

	/// <summary>
	/// Clears all objects from the Pool, allowing them to be collected.
	/// </summary>
	public static void Clear()
	{
		available.Clear();
	}

	/// <summary>
	/// Creates a Default Clear method for various container types
	/// </summary>
	private static Action<T> CreateDefaultClear()
	{
		if (typeof(T).IsAssignableTo(typeof(IList)))
			return static (it) => ((IList)it).Clear();
		if (typeof(T).IsAssignableTo(typeof(IDictionary)))
			return static (it) => ((IDictionary)it).Clear();
		if (typeof(T).IsAssignableTo(typeof(IPoolable)))
			return static (it) => ((IPoolable)it).Recycle();

		// this is all done just so that HashSet automatically clears
		// It has no non-generic interface like IList or IDictionary
		if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(HashSet<>))
		{
			// TODO: should be nameof(HashSet<>.Clear) but that's apparently a preview feature
			var method = typeof(T).GetMethod(nameof(HashSet<int>.Clear));
			if (method != null)
				return (Action<T>)Delegate.CreateDelegate(typeof(Action<T>), method);
		}

		return static (it) => { };
	}
}

/// <summary>
/// Shorthand to Generic Pool because it's nicer to write this way imo
/// </summary>
public static class Pool
{
	public static T Get<[DAM(DAMT.PublicMethods)] T>() where T : class, new()
		=> Pool<T>.Get();

	public static void Return<[DAM(DAMT.PublicMethods)] T>(T instance) where T : class, new()
		=> Pool<T>.Return(instance);
}

/// <summary>
/// Objects retrieved from this pool are returned to the pool at the start of the next frame.
/// </summary>
public static class FramePool<[DAM(DAMT.PublicMethods)] T> where T : class, new()
{
	private static readonly ConcurrentQueue<T> available = new();
	private static readonly ConcurrentBag<T> usedThisFrame = [];

	static FramePool()
		=> FramePool.RegisterNextFrame(NextFrame);

	/// <summary>
	/// Gets an instance from the Pool, or creates one if it doesn't exit
	/// </summary>
	public static T Get()
	{
		if (!available.TryDequeue(out var instance))
			instance = new();
		usedThisFrame.Add(instance);
		return instance;
	}

	/// <summary>
	/// Clears all objects from the Pool, allowing them to be collected.
	/// </summary>
	public static void Clear()
	{
		available.Clear();
		usedThisFrame.Clear();
	}

	/// <summary>
	/// Steps the Pool to the next frame, recycling all objects.
	/// </summary>
	private static void NextFrame()
	{
		while (usedThisFrame.TryTake(out var it))
		{
			Pool<T>.ZeroOut.Invoke(it);
			available.Enqueue(it);
		}
	}
}

public static class FramePool
{
	/// <summary>
	/// Action to perform when stepping to the next frame.
	/// <see cref="FramePool{T}"/> registers its NextFrame function with this event.
	/// </summary>
	private static event Action? OnNextFrame;

	/// <summary>
	/// Shorthand to Generic <see cref="FramePool{T}.Get"/>.
	/// </summary>
	public static T Get<[DAM(DAMT.PublicMethods)] T>() where T : class, new()
		=> FramePool<T>.Get();

	/// <summary>
	/// Steps the Frame Pools to the next frame
	/// </summary>
	internal static void NextFrame()
		=> OnNextFrame?.Invoke();

	/// <summary>
	/// Registers an action to perform on the next frame
	/// </summary>
	internal static void RegisterNextFrame(Action action)
		=> OnNextFrame += action;
}
