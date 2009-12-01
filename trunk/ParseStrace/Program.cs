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

		static Dictionary<int, ProcFDTable> fdTables = new Dictionary<int, ProcFDTable>();
		static IOItemStorage storage;


		static void Main(string[] args)
		{
			string filename, line;
			TextReader reader = GetReader(args, out filename);
			Console.WriteLine("# Parsed Strace: {0}", filename);
			Console.WriteLine("# Date: {0}", DateTime.Now);
			storage = new IOItemStorageVerbose(Console.Out);

			while ((line = reader.ReadLine()) != null)
			{
				Match m = regexLine.Match(line);
				if (!m.Success)
					continue;

				long retvalue;
				if (!long.TryParse(m.Groups[3].Value, out retvalue))
					retvalue = long.Parse(m.Groups[3].Value.Substring(2), NumberStyles.HexNumber);

				if (retvalue < 0)
					continue;

				string command = m.Groups[1].Value.TrimStart('_');
				string arguments = m.Groups[2].Value;
				
				Match mpid = regexPid.Match(line);
				int pid = 0;
				int.TryParse(mpid.Groups[1].Value, out pid);

				ProceedLine(pid, command, arguments, retvalue);
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


		static void ProceedLine(int pid, string cmd, string args, long ret)
		{
			ProcFDTable table;
			if (!fdTables.TryGetValue(pid, out table))
			{
				table = new ProcFDTable(storage);
				fdTables[pid] = table;
			}

			switch (cmd)
			{
				case "fork":
				case "vfork":
				case "clone": fdTables[(int)ret] = table.Fork(); break;

				case "dup":
				case "dup2": table.OnDup(args, ret); break;

				case "open":
				case "creat": table.OnOpen(args, ret); break;

				case "close": table.OnClose(args); break;
				case "lseek": table.OnLSeek(args, ret); break;
				case "read": table.OnReadWrite(false, args, ret); break;
				case "write": table.OnReadWrite(true, args, ret); break;

				default: break;
			}
		}

	}
}
