using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace ParseStrace
{
	class Program
	{
		static Regex regexLine = new Regex(@"(\w+)\((.*)\)\s+= ((0x[\dabcdefABCDEF]+)|(-?\d+))");
		static Regex regexPid = new Regex(@"^(\d+)\s");
		static Regex regexResumed = new Regex(@"<\.\.\. (\w+) resumed> (.+)$");

		static Dictionary<int, ProcFDTable> fdTables = new Dictionary<int, ProcFDTable>();
		static IOItemStorage storage;


		static void Main(string[] args)
		{
			string filename, line;
			TextReader reader = GetReader(args, out filename);
			Console.WriteLine("# Parsed Strace: {0}", filename);
			Console.WriteLine("# Date: {0}", DateTime.Now);
			storage = new IOItemStorageVerbose(Console.Out);
			int count = 0;

			while ((line = reader.ReadLine()) != null)
			{
				if (++count % 10000 == 0)
					Console.Error.WriteLine(count);

				Match mpid = regexPid.Match(line);
				int pid = 0;
				int.TryParse(mpid.Groups[1].Value, out pid);

				Match mresumed;

				if (line.EndsWith("<unfinished ...>"))
				{
					string former = line.Substring(0, line.Length - 16);
					fdTables[pid].OnUnfinished(former);
				}
				else if ((mresumed = regexResumed.Match(line)).Success)
				{
					string former = fdTables[pid].OnResumed();
					string latter = mresumed.Groups[2].Value;
					string whole = former + latter;
					ProceedLine(pid, whole);
				}
				else
				{
					ProceedLine(pid, line);
				}
			}

			storage.Output();
		}


		static TextReader GetReader(string[] args, out string filename)
		{
			if (args.Length >= 1)
			{
				filename = args[0];
				return new StreamReader(args[0]);
			}
			else
			{
				filename = "-";
				return Console.In;
			}
		}


		static void ProceedLine(int pid, string line)
		{
			Match m = regexLine.Match(line);
			if (!m.Success)
				return;

			long ret;
			if (!long.TryParse(m.Groups[3].Value, out ret))
				ret = long.Parse(m.Groups[3].Value.Substring(2), NumberStyles.HexNumber);

			if (ret < 0)
				return;

			string cmd = m.Groups[1].Value.TrimStart('_');
			string args = m.Groups[2].Value;

			ProcFDTable table;
			if (!fdTables.TryGetValue(pid, out table))
			{
				table = new ProcFDTable(pid, storage);
				fdTables[pid] = table;
			}

			switch (cmd)
			{
				case "fork":
				case "vfork":
				case "clone": fdTables[(int)ret] = table.Fork((int)ret); break;

				case "dup":
				case "dup2": table.OnDup(args, ret); break;

				case "open":
				case "creat": table.OnOpen(args, ret); break;

				case "accept": table.OnAccept(args, ret); break;
				case "close": table.OnClose(args); break;
				case "fcntl": table.OnFcntl(args, ret); break;
				case "lseek": table.OnLSeek(args, ret); break;
				case "pipe": table.OnPipe(args); break;
				case "read": table.OnReadWrite(false, args, ret); break;
				case "write": table.OnReadWrite(true, args, ret); break;

				default: break;
			}
		}

	}
}
