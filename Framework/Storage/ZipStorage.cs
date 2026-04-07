using System.IO.Compression;
using System.Text.RegularExpressions;

namespace Foster.Framework;

/// <summary>
/// A Storage Container that reads data from a <see cref="ZipArchive"/>.
/// </summary>
public class ZipStorage : StorageContainer
{
	/// <summary>
	/// ZipArchive's can't be read in parallel, so we need a unique one per thread
	/// as the Asset Loader will load things in various threads.
	/// </summary>
	private readonly ThreadLocal<ZipArchive> zip;
	private readonly Dictionary<string, string> entries = [];

	public override bool Writable => false;

	/// <summary>
	/// Creates a Zip Storage by reading a file from an existing Storage Container.
	/// The Storage Container should not be closed for while the ZipStorage is in use.
	/// </summary>
	public ZipStorage(StorageContainer storage, string file)
	{
		zip = new(() =>
		{
			return new ZipArchive(storage.OpenRead(file), ZipArchiveMode.Read, leaveOpen: false);
		});

		ConstructEntries();
	}

	/// <summary>
	/// Creates a Zip Storage from an arbitrary Stream factory.
	/// Note that the stream factory must to be to create streams across threads,
	/// as internally a <see cref="ZipArchive"/> is created per-thread.
	/// </summary>
	public ZipStorage(Func<Stream> streamFactory)
	{
		zip = new(() =>
		{
			return new ZipArchive(streamFactory(), ZipArchiveMode.Read, leaveOpen: false);
		});

		ConstructEntries();
	}

	/// <summary>
	/// Creates a Zip Storage from a zip file in memory
	/// </summary>
	public ZipStorage(byte[] data)
	{
		zip = new(() =>
		{
			var stream = new MemoryStream(data);
			return new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false);
		});

		ConstructEntries();
	}

	private void ConstructEntries()
	{
		foreach (var it in zip.Value!.Entries)
		{
			var path = Calc.NormalizePath(it.FullName);
			var dir = Path.GetDirectoryName(path);

			// make sure the folder structure exists
			if (!string.IsNullOrEmpty(dir))
			{
				dir = Calc.NormalizePath(dir);

				var parts = dir.Split('/', StringSplitOptions.RemoveEmptyEntries);
				var currentFolder = string.Empty;

				foreach (var part in parts)
				{
					currentFolder = string.IsNullOrEmpty(currentFolder) ? part : $"{currentFolder}/{part}";
					entries.TryAdd($"{currentFolder}/", $"{currentFolder}/"); 
				}
			}

			entries.TryAdd(path, it.FullName);
		}
	}

	public override void Dispose(bool disposing)
	{
		if (disposing)
			zip.Dispose();
	}

	public override bool FileExists(string path)
	{
		return entries.ContainsKey(Calc.NormalizePath(path));
	}

	public override bool DirectoryExists(string path)
	{
		path = Calc.NormalizePath(path);
		if (!path.EndsWith('/'))
			path += '/';
		return entries.ContainsKey(path);
	}

	public override IEnumerable<string> EnumerateDirectory(string? path = null, string? searchPattern = null, SearchOption searchOption = SearchOption.TopDirectoryOnly)
	{
		var archive = zip.Value;
		if (archive == null)
			yield break;

		path ??= string.Empty;
		path = Calc.NormalizePath(path);
		if (!path.EndsWith('/'))
			path += '/';

		Regex? pattern = null;
		if (!string.IsNullOrEmpty(searchPattern))
			pattern = new("^" + Regex.Escape(searchPattern).Replace("\\?", ".").Replace("\\*", ".*") + "$");

		foreach (var entry in entries)
		{
			if (pattern != null)
			{
				var relative = Calc.NormalizePath(Path.GetRelativePath(path, entry.Key));
				if (pattern != null && !pattern.IsMatch(relative))
					continue;
			}
			
			if (!entry.Key.StartsWith(path, StringComparison.OrdinalIgnoreCase))
				continue;
			if (entry.Key.Equals(path, StringComparison.OrdinalIgnoreCase))
				continue;

			if (entry.Key.EndsWith('/'))
				yield return entry.Key[..^1];
			else
				yield return entry.Key;
		}
	}

	public override Stream OpenRead(string path)
	{
		if (entries.TryGetValue(Calc.NormalizePath(path), out var it))
			return zip.Value!.GetEntry(it)!.Open();
		throw new Exception();
	}
}