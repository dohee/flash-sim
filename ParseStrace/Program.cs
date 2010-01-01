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
		static Regex regexResumed = new Regex(@"<\.\.\. (\w+) resumed> (.+)$");

		static Dictionary<int, ProcFDTable> fdTables = new Dictionary<int, ProcFDTable>();
		static IOItemStorage storage;


		static void Main(string[] args)
		{
			string filename = args[0];
			FileStream fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read);
			StreamReader reader = new StreamReader(fileStream);

			Console.WriteLine("# Original Strace: {0}", filename);
			Console.WriteLine("# Parsed by: {0} @ {1}", Environment.UserName, Environment.MachineName);
			Console.WriteLine("# Parsed at: {0}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
			Console.WriteLine("# Parsed with: {0}", Environment.OSVersion);
			storage = new IOItemStorageVerbose(Console.Out);

			ProceedFile(reader, 1);
			fileStream.Seek(0, SeekOrigin.Begin);
			storage.PhaseBetween();
			ProceedFile(reader, 2);
		}

		static void ProceedFile(TextReader reader, int phase)
		{
			string line;
			int lowcount = 0, highcount = 0;
			const int kMaxLow = 10000;

			while ((line = reader.ReadLine()) != null)
			{
				if (++lowcount == kMaxLow)
				{
					lowcount = 0;
					highcount++;
					Console.Error.WriteLine((long)highcount * kMaxLow);
				}

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
			int eqPos = line.LastIndexOf(" = ");
			if (eqPos == -1)
				return;

			string retstring = line.Substring(eqPos + 3).Split(' ')[0];
			long ret;

			if (retstring == "?")
				return;			
			if (!long.TryParse(retstring, out ret))
				ret = long.Parse(retstring.Substring(2), NumberStyles.HexNumber);
			if (ret < 0)
				return;


			int leftBracketPos = line.IndexOf('(');
			int rightBracketPos = line.LastIndexOf(')', eqPos);
			int beforeCmdSpace = line.LastIndexOf(' ', leftBracketPos);

			string cmd = line.Substring(beforeCmdSpace + 1, leftBracketPos - beforeCmdSpace - 1);
			string args = line.Substring(leftBracketPos + 1, rightBracketPos - leftBracketPos - 1);


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
