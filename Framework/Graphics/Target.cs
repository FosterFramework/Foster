using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Foster.Framework;

public class Target : IResource
{
	private static readonly TextureFormat[] defaultFormats = new TextureFormat[] { TextureFormat.Color };

	public string Name { get; set; } = string.Empty;
	public bool IsDisposed => isDisposed;

	public readonly int Width;
	public readonly int Height;
	public readonly ReadOnlyCollection<Texture> Attachments;

	internal readonly IntPtr resource;
	private bool isDisposed = false;

	public Target(int width, int height)
		: this(width, height, defaultFormats) { }

	public Target(int width, int height, TextureFormat[] attachments)
	{
		Debug.Assert(width <= 0 || height <= 0, "Target width and height must be larger than 0");
		Debug.Assert(attachments != null && attachments.Length > 0, "Target needs at least 1 color attachment");

		resource = Platform.FosterTargetCreate(width, height, attachments, attachments.Length);
		if (resource == IntPtr.Zero)
			throw new Exception("Failed to create Target");

		Width = width;
		Height = height;

		var textures = new List<Texture>();
		for (int i = 0; i < attachments.Length; i ++)
		{
			var ptr = Platform.FosterTargetGetAttachment(resource, i);
			textures.Add(new Texture(ptr, width, height, attachments[i]));
		}

		Attachments = textures.AsReadOnly();
	}

	~Target()
	{
		Dispose();
	}

	public void Clear(Color color)
	{
		Clear(color, 0, 0, ClearMask.Color);
	}

	public void Clear(Color color, int depth, int stencil, ClearMask mask)
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

	public void Dispose()
	{
		if (!isDisposed)
		{
			isDisposed = true;
			Platform.FosterTargetDestroy(resource);
		}
	}

	public static implicit operator Texture(Target target) => target.Attachments[0];
}
