using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Gnu.Getopt;

namespace ParseStrace
{
	static class CommandLine
	{
		public static void ShowUsage()
		{
			Console.Error.WriteLine(
@"Usage: {0} [-o <Output>] [-p <PID>[,<PID2>[,...]] [-P <PIDFile>] [<Input>]",
				Utils.GetProgramName());
		}

		public static void ParseArguments(string[] args,
			out string input, out string output, out int[] pids, out string pidfile)
		{
			output = null;
			pids = null;
			pidfile = null;

			Getopt g = new Getopt(Utils.GetProgramName(), args, ":o:p:P:");
			g.Opterr = false;
			int c;

			while ((c = g.getopt()) != -1)
			{
				switch (c)
				{
					case 'o':
						output = g.Optarg;
						break;

					case 'p':
						string[] strs = g.Optarg.Split(',');
						pids = new int[strs.Length];

						for (int i = 0; i < strs.Length; i++)
							pids[i] = int.Parse(strs[i]);

						break;

					case 'P':
						pidfile = g.Optarg;
						break;

					case ':':
						throw new ArgumentException("Uncomplete argument: -" + (char)g.Optopt);
					case '?':
						throw new ArgumentException("Invalid argument: -" + (char)g.Optopt);
					default:
						break;
				}
			}


			if (args.Length > g.Optind)
				input = args[g.Optind];
			else
				input = null;
		}
	}
}