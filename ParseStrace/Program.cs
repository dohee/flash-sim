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




	}
}
