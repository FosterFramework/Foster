using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Foster.Framework;

/// <summary>
/// A 2D Render Target used to draw content off-frame.
/// </summary>
public class Target : IResource
{
	private static readonly TextureFormat[] defaultFormats = new TextureFormat[] { TextureFormat.Color };

	/// <summary>
	/// Optional Target Name
	/// </summary>
	public string Name { get; set; } = string.Empty;

	/// <summary>
	/// Ii the Target has been disposed.
	/// </summary>
	public bool IsDisposed => disposed;

	/// <summary>
	/// The Width of the Target.
	/// </summary>
	public readonly int Width;

	/// <summary>
	/// The Height of the Target.
	/// </summary>
	public readonly int Height;

	/// <summary>
	/// Target Bounds
	/// </summary>
	public readonly RectInt Bounds;

	/// <summary>
	/// The Texture attachments in the Target. 
	/// </summary>
	public readonly ReadOnlyCollection<Texture> Attachments;

	internal readonly IntPtr resource;
	internal bool disposed = false;

	public Target(int width, int height)
		: this(width, height, defaultFormats) { }

	public Target(int width, int height, TextureFormat[] attachments)
	{
		Debug.Assert(width > 0 && height > 0, "Target width and height must be larger than 0");
		Debug.Assert(attachments != null && attachments.Length > 0, "Target needs at least 1 color attachment");

		resource = Platform.FosterTargetCreate(width, height, attachments, attachments.Length);
		if (resource == IntPtr.Zero)
			throw new Exception("Failed to create Target");

		Width = width;
		Height = height;
		Bounds = new RectInt(0, 0, Width, Height);

		var textures = new List<Texture>();
		for (int i = 0; i < attachments.Length; i ++)
		{
			var ptr = Platform.FosterTargetGetAttachment(resource, i);
			textures.Add(new Texture(ptr, width, height, attachments[i]));
		}

		Attachments = textures.AsReadOnly();
		Graphics.Resources.RegisterAllocated(this, resource, Platform.FosterTargetDestroy);
	}

	~Target()
	{
		Dispose(false);
	}

	/// <summary>
	/// Clears the Target to the given color
	/// </summary>
	public void Clear(Color color)
	{
		Clear(color, 0, 0, ClearMask.Color);
	}

	/// <summary>
	/// Clears the Target
	/// </summary>
	public void Clear(Color color, float depth, int stencil, ClearMask mask)
	{
		Debug.Assert(!IsDisposed, "Target is Disposed");

		Platform.FosterClearCommand clear = new()
		{
			target = resource,
			clip = new(0, 0, Width, Height),
			color = color,
			depth = depth,
			stencil = stencil,
			mask = mask
		};
		
		Platform.FosterClear(ref clear);
	}

	/// <summary>
	/// Disposes of the Target and all its Attachments
	/// </summary>
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	private void Dispose(bool disposing)
	{
		if (!disposed)
		{
			if (disposing)
			{
				foreach (var attachment in Attachments)
					attachment.disposed = true;
			}

			Graphics.Resources.RequestDelete(resource);
			disposed = true;
		}
	}

	public static implicit operator Texture(Target target) => target.Attachments[0];
}
