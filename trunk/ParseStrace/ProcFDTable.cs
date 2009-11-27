using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace ParseStrace
{
	class ProcFDTable
	{
		private static Regex regexOpen = new Regex(@"^\""(.+)\"",");
		private static Regex regexFirstArg = new Regex(@"^(\d+),");

		private TextWriter writer;
		private List<IOItem> records = new List<IOItem>();
		private Dictionary<int, FileState> curFiles = new Dictionary<int, FileState>();


		public ProcFDTable(TextWriter writer)
		{
			this.writer = writer;
		}

		public ProcFDTable Fork()
		{
			ProcFDTable other = new ProcFDTable(writer);

			foreach (var item in curFiles)
				other.curFiles.Add(item.Key, item.Value);

			return other;
		}


		public void OnOpen(string args, long ret)
		{
			int fd = (int)ret;
			string filename = regexOpen.Match(args).Groups[1].Value;
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

			Debug.Assert(curFiles.ContainsKey(fd));
			FileState fs = curFiles[fd];
			/*FileState fs;

			if (!curFiles.TryGetValue(fd, out fs))
			{
				fs = new FileState(fd.ToString());
				curFiles[fd] = fs;
			}*/

			IOItem item = new IOItem(fs.Filename, isWrite, fs.Position, ret);
			records.Add(item);
			fs.Position += ret;
			Output(item);
		}

		public void OnLSeek(string args, long ret)
		{
			int fd = int.Parse(regexFirstArg.Match(args).Groups[1].Value);
			Debug.Assert(curFiles.ContainsKey(fd));

			FileState fs = curFiles[fd];
			fs.Position = ret;
		}


		private void Output(IOItem item)
		{
			writer.WriteLine("{0}\t{1}\t{2}\t{3}",
				item.Position, item.Length,
				item.IsWrite ? 1 : 0, item.Filename);
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
