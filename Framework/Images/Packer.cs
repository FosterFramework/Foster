using System.Runtime.InteropServices;

namespace Foster.Framework;

/// <summary>
/// The Packer takes source image data and packs them into large texture
/// pages that can then be used for Atlases. This is useful for sprite fonts,
/// sprite sheets, etc.
/// </summary>
public class Packer
{
	/// <summary>
	/// A single packed Entry
	/// </summary>
	/// <param name="Index">Index when added to the Packer</param>
	/// <param name="Name">The Name of the Entry</param>
	/// <param name="Page">The corresponding image page of the Entry</param>
	/// <param name="Source">The Source Rectangle</param>
	/// <param name="Frame">The Frame Rectangle. This is the size of the image before it was packed</param>
	public readonly record struct Entry
	(
		int Index,
		string Name,
		int Page,
		RectInt Source,
		RectInt Frame
	);

	/// <summary>
	/// Stores the Packed result of the Packer
	/// </summary>
	public readonly struct Output()
	{
		public readonly List<Image> Pages = [];
		public readonly List<Entry> Entries = [];
	}

	/// <summary>
	/// Whether to trim transparency from the source images
	/// </summary>
	public bool Trim = true;

	/// <summary>
	/// Maximum Texture Size. If the packed data is too large it will be split into multiple pages
	/// </summary>
	public int MaxSize = 8192;

	/// <summary>
	/// Pixel padding around each packed entry.
	/// </summary>
	public int Padding = 1;

	/// <summary>
	/// Edge pixels are copied into the padding (requires <see cref="Padding"/> >= 2) <br/>
	/// </summary>
	public bool DuplicateEdges = false;

	/// <summary>
	/// Forces the resulting packed images to be a power of two.
	/// </summary>
	public bool PowerOfTwo = false;

	/// <summary>
	/// This will check each image to see if it's a duplicate of an already packed image.
	/// It will still add the entry, but not the duplicate image data.
	/// </summary>
	public bool CombineDuplicates = false;

	/// <summary>
	/// The total number of source images
	/// </summary>
	public int SourceImageCount => sources.Count;

	private struct Source(int index, string name)
	{
		public int Index = index;
		public int Hash;
		public string Name = name;
		public RectInt Packed;
		public RectInt Frame;
		public int BufferIndex;
		public int BufferLength;
		public int? DuplicateOf;
		public readonly bool Empty => Packed.Width <= 0 || Packed.Height <= 0;
	}

	private readonly List<Source> sources = [];
	private Color[] sourceBuffer = new Color[32];
	private int sourceBufferIndex = 0;

	public int Add(string name, Image image)
	{
		return Add(name, image.Width, image.Height, image.Data);
	}

	public int Add(string name, Image image, RectInt clip)
	{
		return Add(sources.Count, name, clip, image.Width, image.Data);
	}

	public int Add(string name, string path)
	{
		using var image = new Image(path);
		return Add(name, image);
	}

	public int Add(string name, int width, int height, ReadOnlySpan<Color> pixels)
	{
		return Add(sources.Count, name, width, height, pixels);
	}

	public int Add(int index, string name, int width, int height, ReadOnlySpan<Color> pixels)
	{
		return Add(index, name, new RectInt(0, 0, width, height), width, pixels);
	}

	public int Add(int index, string name, RectInt clip, int stride, ReadOnlySpan<Color> pixels)
	{
		var source = new Source(index, name);
		int top = clip.Top, left = clip.Left, right = clip.Right, bottom = clip.Bottom;

		// trim
		if (Trim)
		{
			// TOP:
			for (int y = clip.Top; y < clip.Bottom; y++)
				for (int x = clip.Left, s = left + y * stride; x < clip.Right; x++, s++)
					if (pixels[s].A > 0)
					{
						top = y;
						goto LEFT;
					}
				LEFT:
			for (int x = clip.Left; x < clip.Right; x++)
				for (int y = top, s = x + y * stride; y < clip.Bottom; y++, s += stride)
					if (pixels[s].A > 0)
					{
						left = x;
						goto RIGHT;
					}
				RIGHT:
			for (int x = clip.Right - 1; x >= left; x--)
				for (int y = top, s = x + y * stride; y < clip.Bottom; y++, s += stride)
					if (pixels[s].A > 0)
					{
						right = x + 1;
						goto BOTTOM;
					}
				BOTTOM:
			for (int y = clip.Bottom - 1; y >= top; y--)
				for (int x = left, s = left + y * stride; x < right; x++, s++)
					if (pixels[s].A > 0)
					{
						bottom = y + 1;
						goto END;
					}
				END:;
		}

		// determine sizes
		// there's a chance this image was empty in which case we have no width / height
		if (left <= right && top <= bottom)
		{
			if (CombineDuplicates)
			{
				source.Hash = 0;
				for (int x = left; x < right; x++)
					for (int y = top; y < bottom; y++)
						source.Hash = ((source.Hash << 5) + source.Hash) + (int)pixels[x + y * stride].RGBA;

				for (int i = 0; i < sources.Count; i++)
					if (sources[i].Hash == source.Hash)
					{
						source.DuplicateOf = sources[i].Index;
						break;
					}
			}

			source.Packed = new RectInt(0, 0, right - left, bottom - top);
			source.Frame = new RectInt(clip.Left - left, clip.Top - top, clip.Width, clip.Height);

			if (!source.DuplicateOf.HasValue)
			{
				var append = source.Packed.Width * source.Packed.Height;
				while (sourceBufferIndex + append >= sourceBuffer.Length)
					Array.Resize(ref sourceBuffer, sourceBuffer.Length * 2);

				source.BufferIndex = sourceBufferIndex;
				source.BufferLength = append;

				// copy our trimmed pixel data to the main buffer
				for (int i = 0; i < source.Packed.Height; i++)
				{
					var len = source.Packed.Width;
					var srcIndex = left + (top + i) * stride;
					var dstIndex = sourceBufferIndex;
					var srcData = pixels.Slice(srcIndex, len);
					var dstData = sourceBuffer.AsSpan(dstIndex, len);

					srcData.CopyTo(dstData);
					sourceBufferIndex += len;
				}
			}
		}
		else
		{
			source.Packed = new RectInt();
			source.Frame = new RectInt(0, 0, clip.Width, clip.Height);
		}

		sources.Add(source);
		return source.Index;
	}

	public unsafe Output Pack()
	{
		Output result = new();

		// Nothing to pack
		if (sources.Count <= 0)
			return result;

		int packed = 0;
		int page = 0;

		// sort the sources by size
		sources.Sort((a, b) => b.Packed.Width * b.Packed.Height - a.Packed.Width * a.Packed.Height);

		// make sure the largest isn't too large
		if (sources[0].Packed.Width > MaxSize || sources[0].Packed.Height > MaxSize)
			throw new Exception("Source image is larger than max atlas size");

		// move empty / duplicates to the start - we skip them during packing
		for (int i = 0; i < sources.Count; i ++)
		{
			if (sources[i].Empty || sources[i].DuplicateOf.HasValue)
			{
				(sources[i], sources[packed]) = (sources[packed], sources[i]);
				packed++;
			}
		}

		var padding = Math.Max(0, Padding);
		var algo = new DefaultAlgorithm();

		while (packed < sources.Count)
		{
			var remaining = CollectionsMarshal.AsSpan(sources)[packed..];
			var res = algo.Pack(remaining, padding, MaxSize);

			// get page size
			int pageWidth = res.Width, 
				pageHeight = res.Height;

			if (PowerOfTwo)
			{
				pageWidth = pageHeight = 2;
				while (pageWidth < res.Width) pageWidth *= 2;
				while (pageHeight < res.Height) pageHeight *= 2;
			}

			// create page data
			var bmp = new Image(pageWidth, pageHeight);
			result.Pages.Add(bmp);

			// create each entry for this page and copy its image data
			for (int i = 0; i < res.Packed; i++)
			{
				var source = remaining[i];
				result.Entries.Add(new(source.Index, source.Name, page, source.Packed, source.Frame));

				var data = sourceBuffer.AsSpan(source.BufferIndex, source.BufferLength);
				bmp.CopyPixels(data, source.Packed.Width, source.Packed.Height, source.Packed.Position);

				if (DuplicateEdges && padding >= 2)
				{
					var p = source.Packed;
					bmp.CopyPixels(bmp, new RectInt(p.Position, new Point2(1, p.Height)), p.Position + new Point2(-1, 0)); // L
					bmp.CopyPixels(bmp, new RectInt(p.Position + new Point2(p.Width - 1, 0), new Point2(1, p.Height)), p.Position + new Point2(p.Width, 0)); // R
					bmp.CopyPixels(bmp, new RectInt(p.Position + new Point2(-1, 0), new Point2(p.Width + 2, 1)), p.Position + new Point2(-1, -1)); // T
					bmp.CopyPixels(bmp, new RectInt(p.Position + new Point2(-1, p.Height - 1), new Point2(p.Width + 2, 1)), p.Position + new Point2(-1, p.Height)); // B
				}
			}

			packed += res.Packed;
			page++;
		}

		// add empty sources
		foreach (var source in sources)
			if (source.Empty)
				result.Entries.Add(new(source.Index, source.Name, page, source.Packed, source.Frame));

		// make sure duplicates have entries
		if (CombineDuplicates)
		{
			foreach (var source in sources)
			{
				if (!source.DuplicateOf.HasValue)
					continue;

				foreach (var entry in result.Entries)
					if (entry.Index == source.DuplicateOf.Value)
					{
						result.Entries.Add(new(source.Index, source.Name, entry.Page, entry.Source, source.Frame));
						break;
					}
			}
		}

		return result;
	}

	/// <summary>
	/// Clears all source data
	/// </summary>
	public void Clear()
	{
		sources.Clear();
		sourceBufferIndex = 0;
	}

	private abstract class PackingAlgorithm
	{
		public readonly record struct Result(int Packed, int Width, int Height);
		public abstract Result Pack(Span<Source> entries, int padding, int maxSize);
	}

	private class DefaultAlgorithm : PackingAlgorithm
	{
		private struct PackingNode
		{
			public bool Used;
			public RectInt Rect;
			public unsafe PackingNode* Right;
			public unsafe PackingNode* Down;
		};

		private PackingNode[] buffer = [];

		public override unsafe Result Pack(Span<Source> sources, int padding, int maxSize)
		{
			int nodeCount = sources.Length * 4;
			if (buffer.Length < nodeCount)
				Array.Resize(ref buffer, nodeCount);

			var packed = 0;
			var from = packed;
			var halfPadding = padding / 2;
			int width = 0, height = 0;
			
			fixed (PackingNode* nodes = buffer)
			{
				var nodePtr = nodes;
				var rootPtr = ResetNode(nodePtr++, 0, 0, sources[from].Packed.Width + padding, sources[from].Packed.Height + padding);

				while (packed < sources.Length)
				{
					int w = sources[packed].Packed.Width + padding;
					int h = sources[packed].Packed.Height + padding;
					var node = FindNode(rootPtr, w, h);

					// try to expand
					if (node == null)
					{
						bool canGrowDown = (w <= rootPtr->Rect.Width) && (rootPtr->Rect.Height + h < maxSize);
						bool canGrowRight = (h <= rootPtr->Rect.Height) && (rootPtr->Rect.Width + w < maxSize);
						bool shouldGrowRight = canGrowRight && (rootPtr->Rect.Height >= (rootPtr->Rect.Width + w));
						bool shouldGrowDown = canGrowDown && (rootPtr->Rect.Width >= (rootPtr->Rect.Height + h));

						if (canGrowDown || canGrowRight)
						{
							// grow right
							if (shouldGrowRight || (!shouldGrowDown && canGrowRight))
							{
								var next = ResetNode(nodePtr++, 0, 0, rootPtr->Rect.Width + w, rootPtr->Rect.Height);
								next->Used = true;
								next->Down = rootPtr;
								next->Right = node = ResetNode(nodePtr++, rootPtr->Rect.Width, 0, w, rootPtr->Rect.Height);
								rootPtr = next;
							}
							// grow down
							else
							{
								var next = ResetNode(nodePtr++, 0, 0, rootPtr->Rect.Width, rootPtr->Rect.Height + h);
								next->Used = true;
								next->Down = node = ResetNode(nodePtr++, 0, rootPtr->Rect.Height, rootPtr->Rect.Width, h);
								next->Right = rootPtr;
								rootPtr = next;
							}
						}
					}

					// doesn't fit in this page
					if (node == null)
						break;

					// add
					node->Used = true;
					node->Down = ResetNode(nodePtr++, node->Rect.X, node->Rect.Y + h, node->Rect.Width, node->Rect.Height - h);
					node->Right = ResetNode(nodePtr++, node->Rect.X + w, node->Rect.Y, node->Rect.Width - w, h);

					var it = sources[packed];
					it.Packed.X = node->Rect.X + halfPadding;
					it.Packed.Y = node->Rect.Y + halfPadding;
					sources[packed] = it;

					packed++;
				}

				// get page size
				width = rootPtr->Rect.Width;
				height = rootPtr->Rect.Height;
			}

			return new (packed, width, height);
		}

		private static unsafe PackingNode* FindNode(PackingNode* root, int w, int h)
		{
			if (root->Used)
			{
				var r = FindNode(root->Right, w, h);
				return (r != null ? r : FindNode(root->Down, w, h));
			}
			else if (w <= root->Rect.Width && h <= root->Rect.Height)
			{
				return root;
			}

			return null;
		}

		private static unsafe PackingNode* ResetNode(PackingNode* node, int x, int y, int w, int h)
		{
			node->Used = false;
			node->Rect = new RectInt(x, y, w, h);
			node->Right = null;
			node->Down = null;
			return node;
		}
	}
}
