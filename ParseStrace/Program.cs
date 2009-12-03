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
		static Regex regexLine = new Regex(@"(\w+)\((.*)\)\s+= ([^ ]+)");
		static Regex regexResumed = new Regex(@"<\.\.\. (\w+) resumed> (.+)$");

		static Dictionary<int, ProcFDTable> fdTables = new Dictionary<int, ProcFDTable>();
		static IOItemStorage storage;


		static void Main(string[] args)
		{
			string filename = args[0];
			FileStream fileStream = new FileStream(filename, FileMode.Open);

			Console.WriteLine("# Parsed Strace: {0}", filename);
			Console.WriteLine("# Date: {0}", DateTime.Now);
			storage = new IOItemStorageVerbose(Console.Out);

			StreamReader reader = new StreamReader(fileStream);
			ProceedFile(reader, 1);
			storage.PhaseBetween();
			fileStream.Seek(0, SeekOrigin.Begin);
			ProceedFile(reader, 2);
		}

		static void ProceedFile(TextReader reader, int phase)
		{
			string line;
			int count = 0;

			while ((line = reader.ReadLine()) != null)
			{
				if (++count % 10000 == 0)
					Console.Error.WriteLine(count);

				int firstSpace = line.IndexOf(' ');
				int pid = int.Parse(line.Substring(0, firstSpace));

				Match mresumed;

				if (line.EndsWith("<unfinished ...>"))
				{
					string former = line.Substring(0, line.Length - 16);
					fdTables[pid].OnUnfinished(former);
				}
				else if (line.Contains("resumed> ") && (mresumed = regexResumed.Match(line)).Success)
				{
					string former = fdTables[pid].OnResumed();
					string latter = mresumed.Groups[2].Value;
					string whole = former + latter;
					ProceedLine(pid, whole, phase);
				}
				else
				{
					ProceedLine(pid, line, phase);
				}
			}
		}


		static void ProceedLine(int pid, string line, int phase)
		{
			Match m = regexLine.Match(line);
			if (!m.Success)
				return;

			string retstring = m.Groups[3].Value;
			if (retstring == "?")
				return;

			long ret;
			if (!long.TryParse(retstring, out ret))
				ret = long.Parse(retstring.Substring(2), NumberStyles.HexNumber);
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
				case "read": table.OnReadWrite(false, args, ret, phase); break;
				case "write": table.OnReadWrite(true, args, ret, phase); break;

				default: break;
			}
		}

	}
}
