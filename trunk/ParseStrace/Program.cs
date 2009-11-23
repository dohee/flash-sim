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
		static Regex regexLine = new Regex(@"(\w+)\((.+)\)\s+= (-?\d+)");
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

				switch (command)
				{
					case "open": OnOpen(arguments, retvalue); break;
					case "creat": OnOpen(arguments, retvalue); break;
					case "close": OnClose(arguments); break;
					case "read": OnReadWrite(false, arguments, retvalue); break;
					case "write": OnReadWrite(true, arguments, retvalue); ; break;
					case "lseek": OnLSeek(arguments, retvalue); break;
					case "llseek": OnLLSeek(arguments, retvalue); break;
					default: break;
				}
			}

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
		static Dictionary<int, FileState> curFiles = new Dictionary<int, FileState>();

		static void OnOpen(string args, long ret)
		{
			int fd = (int)ret;
			string filename = regexOpen.Match(args).Groups[1].Value;
			Debug.Assert(!curFiles.ContainsKey(fd));
			curFiles[fd] = new FileState(filename);
		}

		static void OnClose(string args)
		{
			int fd = int.Parse(args);			
			curFiles.Remove(fd);
		}

		static void OnReadWrite(bool isWrite, string args, long ret)
		{
			int fd = int.Parse(regexFirstArg.Match(args).Groups[1].Value);
			FileState fs;

			if (!curFiles.TryGetValue(fd, out fs))
			{
				fs = new FileState(fd.ToString());
				curFiles[fd] = fs;
			}

			records.Add(new IOItem(fs.Filename, isWrite, fs.Position, ret));
			fs.Position += ret;
		}

		static void OnLSeek(string args, long ret)
		{
			int fd = int.Parse(regexFirstArg.Match(args).Groups[1].Value);
			Debug.Assert(curFiles.ContainsKey(fd));

			FileState fs = curFiles[fd];
			fs.Position = ret;
		}

		static void OnLLSeek(string args, long ret)
		{
			Debug.Assert(false);
		}


	}
}
