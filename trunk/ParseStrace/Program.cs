using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace ParseStrace
{
	class Program
	{
		static Regex regexLine = new Regex(@"(\w+)\((.+)\)\s+= ((0x[\dabcdefABCDEF]+)|(-?\d+))");
		static Regex regexPid = new Regex(@"^(\d+)\s");
		static Regex regexOpen = new Regex(@"^\""(.+)\"",");
		static Regex regexFirstArg = new Regex(@"^(\d+),");


		static void Main(string[] args)
		{
			TextReader reader = GetReader(args);
			string line;

			while ((line = reader.ReadLine()) != null)
			{
				Match m = regexLine.Match(line);
				if (!m.Success)
					continue;

				long retvalue = long.Parse(m.Groups[3].Value);

				if (retvalue < 0)
					continue;

				string command = m.Groups[1].Value.TrimStart('_');
				string arguments = m.Groups[2].Value;
				
				Match mpid = regexPid.Match(line);
				int pid = 0;
				int.TryParse(mpid.Groups[1].Value, out pid);


				switch (command)
				{
					case "open": OnOpen(pid, arguments, retvalue); break;
					case "creat": OnOpen(pid, arguments, retvalue); break;
					case "close": OnClose(pid, arguments); break;
					case "read": OnReadWrite(false, pid, arguments, retvalue); break;
					case "write": OnReadWrite(true, pid, arguments, retvalue); break;
					case "lseek": OnLSeek(pid, arguments, retvalue); break;
					case "llseek": OnLLSeek(pid, arguments, retvalue); break;
					default: break;
				}
			}

			Output(Console.Out);
		}


		static TextReader GetReader(string[] args)
		{
			if (args.Length >= 1)
				return new StreamReader(args[0]);
			else
				return Console.In;
		}




		struct IOItem
		{
			public readonly string Filename;
			public readonly bool IsWrite;
			public readonly long Position;
			public readonly long Length;

			public IOItem(string filename, bool isWrite, long pos, long length)
			{
				Filename = filename;
				IsWrite = isWrite;
				Position = pos;
				Length = length;
			}
		}

		struct FileDesc
		{
			public readonly int PID;
			public readonly int FD;

			public FileDesc(int pid, int fd)
			{
				PID = pid;
				FD = fd;
			}

			//TODO eq
		}

		class FileState
		{
			public readonly string Filename;
			public long Position = 0;

			public FileState(string filename)
			{
				Filename = filename;
			}
		}


		static List<IOItem> records = new List<IOItem>();
		static Dictionary<FileDesc, FileState> curFiles = new Dictionary<FileDesc, FileState>();

		static void OnOpen(int pid, string args, long ret)
		{
			FileDesc fd = new FileDesc(pid, (int)ret);
			string filename = regexOpen.Match(args).Groups[1].Value;
			Debug.Assert(!curFiles.ContainsKey(fd));
			curFiles[fd] = new FileState(filename);
		}
		static void OnClose(int pid, string args)
		{
			FileDesc fd = new FileDesc(pid, int.Parse(args));
			curFiles.Remove(fd);
		}
		static void OnReadWrite(bool isWrite, int pid, string args, long ret)
		{
			int fdnum = int.Parse(regexFirstArg.Match(args).Groups[1].Value);
			if (fdnum < 3)
				return;

			FileDesc fd = new FileDesc(pid, fdnum);
			FileState fs;

			if (!curFiles.TryGetValue(fd, out fs))
			{
				fs = new FileState(fd.ToString());
				curFiles[fd] = fs;
			}

			records.Add(new IOItem(fs.Filename, isWrite, fs.Position, ret));
			fs.Position += ret;
		}
		static void OnLSeek(int pid, string args, long ret)
		{
			int fdnum = int.Parse(regexFirstArg.Match(args).Groups[1].Value);
			FileDesc fd = new FileDesc(pid, fdnum);
			Debug.Assert(curFiles.ContainsKey(fd));

			FileState fs = curFiles[fd];
			fs.Position = ret;
		}
		static void OnLLSeek(int pid, string args, long ret)
		{
			Debug.Assert(false);
		}


		static void Output(TextWriter writer)
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
	}
}
