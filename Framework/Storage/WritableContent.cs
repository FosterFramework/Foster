using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Foster.Framework.Storage
{
	/// <summary>
	/// A Content that may also be written to.
	/// </summary>
	public class WritableContent : Content
	{
		public WritableContent() { }
		public WritableContent(string currentDirectory) : base(currentDirectory) { }

		#region Directory
		public virtual void CreateDirectory(string path)
		{
			Directory.CreateDirectory(CurrentDirectory + path);
		}

		public virtual void DeleteDirectory(string path, bool recursive)
		{
			Directory.Delete(CurrentDirectory + path, recursive);
		}

		public virtual void DeleteFile(string path)
		{
			File.Delete(CurrentDirectory + path);
		}

		#endregion

		#region File

		public virtual Stream OpenWrite(string path)
		{
			return File.OpenWrite(CurrentDirectory + path);
		}

		public virtual void WriteAllBytes(string path, byte[] bytes)
		{
			File.WriteAllBytes(CurrentDirectory + path, bytes);
		}

		public virtual void WriteAllText(string path, string text)
		{
			File.WriteAllText(CurrentDirectory + path, text);
		}

		#endregion
	}
}
