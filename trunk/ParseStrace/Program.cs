using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace ParseStrace
{
	class Program
	{
		delegate void PassIOItem(IOItem ioitem);

		static Regex regexResumed = new Regex(@"<\.\.\. (\w+) resumed> (.+)$");
		static Dictionary<int, ProcFDTable> fdTables = new Dictionary<int, ProcFDTable>();


		static void Main(string[] args)
		{
			string filename = args[0];
			const int bufferSize = 8 * 1024 * 1024;
			TextWriter output = Console.Out;
			IOItemFormatter formatter = null;

			try
			{
				if (args.Length >= 2)
					output = new StreamWriter(args[1], false, Encoding.Default, bufferSize);

				formatter = new IOItemVerboseFormatter(output);
				formatter.PhaseBefore(new FormatterInfo(filename));
				Console.Error.WriteLine("Phase 1:");

				using (StreamReader reader = new StreamReader(filename, Encoding.Default, true, bufferSize))
					ProceedFile(reader, formatter.PhaseOne);
				
				formatter.PhaseBetween();
				Console.Error.WriteLine("Phase 2:");

				using (StreamReader reader = new StreamReader(filename, Encoding.Default, true, bufferSize))
					ProceedFile(reader, formatter.PhaseTwo);

				formatter.PhaseAfter();
			}
			finally
			{
				output.Dispose();
			}
		}

		static void ProceedFile(TextReader reader, PassIOItem passfunc)
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

					Console.Error.Write("\rProcessed {0} lines.          ",
						(long)highcount * kMaxLow);
					Console.Error.Flush();
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

					IOItem item = ProceedLine(pid, whole);
					if (item != null)
						passfunc(item);
				}
				else
				{
					IOItem item = ProceedLine(pid, line);
					if (item != null)
						passfunc(item);
				}
			}

			Console.Error.WriteLine("\rProcessed {0} lines.          ",
				(long)highcount * kMaxLow + lowcount);
		}


		static IOItem ProceedLine(int pid, string line)
		{
			int eqPos = line.LastIndexOf(" = ");
			if (eqPos == -1)
				return null;

			string retstring = line.Substring(eqPos + 3).Split(' ')[0];
			long ret;

			if (retstring == "?")
				return null;
			ret = retstring.ParseHexLong();
			if (ret < 0)
				return null;


			int leftBracketPos = line.IndexOf('(');
			int rightBracketPos = line.LastIndexOf(')', eqPos);
			int beforeCmdSpace = line.LastIndexOf(' ', leftBracketPos);

			string cmd = line.Substring(beforeCmdSpace + 1, leftBracketPos - beforeCmdSpace - 1);
			string args = line.Substring(leftBracketPos + 1, rightBracketPos - leftBracketPos - 1);


			ProcFDTable table;
			if (!fdTables.TryGetValue(pid, out table))
			{
				table = new ProcFDTable(pid);
				fdTables[pid] = table;
			}

			switch (cmd)
			{
				case "fork":
				case "vfork":
				case "clone": fdTables[(int)ret] = table.Fork((int)ret); return null;

				case "dup":
				case "dup2": return table.OnDup(args, ret);

				case "open":
				case "creat": return table.OnOpen(args, ret);

				case "mmap": return table.OnMmap(1, args);
				case "mmap2": return table.OnMmap(4096, args);
				case "mremap":
				case "msync":
				case "munmap": return null;

				case "read": return table.OnReadWrite(AccessRoutine.Read, args, ret);
				case "readv": return table.OnReadWrite(AccessRoutine.Readv, args, ret);
				case "pread": return table.OnReadWrite(AccessRoutine.Pread, args, ret);
				case "write": return table.OnReadWrite(AccessRoutine.Write, args, ret);
				case "writev": return table.OnReadWrite(AccessRoutine.Writev, args, ret);
				case "pwrite": return table.OnReadWrite(AccessRoutine.Pwrite, args, ret);
					
				case "accept": return table.OnAccept(args, ret);
				case "close": return table.OnClose(args);
				case "fcntl": return table.OnFcntl(args, ret);
				case "lseek": return table.OnLSeek(args, ret);
				case "pipe": return table.OnPipe(args);

				default: return null;
			}

		}
	}
}
