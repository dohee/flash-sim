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
		private static readonly Regex regexOpen = new Regex(@"^\""(.+)\"",");
		private static readonly Regex regexFirstArg = new Regex(@"^(\d+),");
		private static readonly Regex regexSelfDir = new Regex(@"/\./");
		private static readonly Regex regexParentDir = new Regex(@"/(([^./])|(\.[^./])|(\.\.[^/]))[^/]*/../");

		private IOItemStorage storage;
		private Dictionary<int, FileState> curFiles = new Dictionary<int, FileState>();


		public ProcFDTable(IOItemStorage storage)
		{
			this.storage = storage;
		}

		public ProcFDTable Fork()
		{
			ProcFDTable other = new ProcFDTable(storage);

			foreach (var item in curFiles)
				other.curFiles.Add(item.Key, item.Value);

			return other;
		}


		public void OnOpen(string args, long ret)
		{
			int fd = (int)ret;
			string filename = regexOpen.Match(args).Groups[1].Value;
			curFiles[fd] = new FileState(NormalizeFilename(filename));
		}

		private string NormalizeFilename(string filename)
		{
			string oldfilename = null;
			while (oldfilename != filename)
			{
				oldfilename = filename;
				filename = regexSelfDir.Replace(oldfilename, "/");
			}

			oldfilename = null;
			while (oldfilename != filename)
			{
				oldfilename = filename;
				filename = regexParentDir.Replace(oldfilename, "/");
			}

			return filename;
		}


		public void OnClose(string args)
		{
			int fd = int.Parse(args);
			curFiles.Remove(fd);
		}

		public void OnReadWrite(bool isWrite, string args, long ret)
		{
			int fd = int.Parse(regexFirstArg.Match(args).Groups[1].Value);
			
			FileState fs;
			if (!curFiles.TryGetValue(fd, out fs))
			{
				fs = new FileState("Unknown-fd:" + fd.ToString());
				curFiles[fd] = fs;
			}

			IOItem item = new IOItem(fs.Filename, isWrite, fs.Position, ret);
			fs.Position += ret;
			storage.Add(item);
		}

		public void OnLSeek(string args, long ret)
		{
			int fd = int.Parse(regexFirstArg.Match(args).Groups[1].Value);
			Debug.Assert(curFiles.ContainsKey(fd));

			FileState fs = curFiles[fd];
			fs.Position = ret;
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
