using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace ParseStrace
{
	class ProcFDTable
	{
		private static Regex regexOpen = new Regex(@"^\""(.+)\"",");
		private static Regex regexFirstArg = new Regex(@"^(\d+),");

		private List<IOItem> records = new List<IOItem>();
		private Dictionary<int, FileState> curFiles = new Dictionary<int, FileState>();


		public void OnOpen(string args, long ret)
		{
			int fd = (int)ret;
			string filename = regexOpen.Match(args).Groups[1].Value;
			Debug.Assert(!curFiles.ContainsKey(fd));
			curFiles[fd] = new FileState(filename);
		}

		public void OnClose(string args)
		{
			int fd = int.Parse(args);
			curFiles.Remove(fd);
		}

		public void OnReadWrite(bool isWrite, string args, long ret)
		{
			int fd = int.Parse(regexFirstArg.Match(args).Groups[1].Value);
			if (fd < 3)
				return;

			FileState fs;

			if (!curFiles.TryGetValue(fd, out fs))
			{
				fs = new FileState(fd.ToString());
				curFiles[fd] = fs;
			}

			records.Add(new IOItem(fs.Filename, isWrite, fs.Position, ret));
			fs.Position += ret;
		}

		public void OnLSeek(string args, long ret)
		{
			int fd = int.Parse(regexFirstArg.Match(args).Groups[1].Value);
			Debug.Assert(curFiles.ContainsKey(fd));

			FileState fs = curFiles[fd];
			fs.Position = ret;
		}

		public void OnLLSeek(string args, long ret)
		{
			Debug.Assert(false);
		}


		public void Output(TextWriter writer)
		{
			//TODO fd
			Dictionary<string, int> fileIndex = new Dictionary<string, int>();
			const int kPagesize = 4096;

			foreach (IOItem item in records)
			{
				int index;
				if (!fileIndex.TryGetValue(item.Filename, out index))
				{
					index = fileIndex.Count;
					fileIndex[item.Filename] = index;
				}

				Debug.Assert(index < kPagesize);
				int pos = (int)(item.Position / kPagesize);
				int pos2 = (int)((item.Position + item.Length) / kPagesize);
				int len = pos2 - pos + 1;
				pos += index * (int)(0x80000000L / kPagesize);

				writer.WriteLine("{0}\t{1}\t{2}", pos, len, item.IsWrite ? 1 : 0);
			}
		}


		private class FileState
		{
			public readonly string Filename;
			public long Position = 0;

			public FileState(string filename)
			{
				Filename = filename;
			}
		}
	}
}
