using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Gnu.Getopt;

namespace Buffers.Program
{
	static class CommandLine
	{
		public static void ShowUsage()
		{
			Console.Error.WriteLine(
@"Usage: {0} [-r <ReadCost>] [-w <WriteCost>] [-c]
         -a <Algorithm>[,<Algorithm2>[,...]]
         -p <NPages>[,<NPages2>[,...]]
         [<TraceFile>]
       {0} [-r <ReadCost>] [-w <WriteCost>] -f <FileToOperate>
         -a <Algorithm> -p <NPages>
         [<TraceFile>]",
				Utils.GetProgramName());
		}

		public static void ParseArguments(string[] args, out string filename,
			out decimal readCost, out decimal writeCost, out uint[] npageses,
			out AlgorithmSpec[] algorithms, out bool verify, out string opfile)
		{
			readCost = 80;
			writeCost = 200;
			npageses = new uint[] { 1024 };
			verify = false;
			opfile = null;

			Regex regexAlgo = new Regex(@"(\w+)(?:\(([^)]+)\))?");
			List<AlgorithmSpec> algos = new List<AlgorithmSpec>();

			Getopt g = new Getopt(Utils.GetProgramName(), args, ":a:cp:r:w:");
			g.Opterr = false;
			int c;

			while ((c = g.getopt()) != -1)
			{
				switch (c)
				{
					case 'a':
						foreach (Match m in regexAlgo.Matches(g.Optarg))
						{
							string name = m.Groups[1].Value;
							string[] arg = m.Groups[2].Value.Split(',');

							if (m.Groups[2].Success)
								algos.Add(new AlgorithmSpec(name, arg));
							else
								algos.Add(new AlgorithmSpec(name));
						}
						break;

					case 'c':
						verify = true;
						break;

					case 'f':
						opfile = g.Optarg;
						break;

					case 'p':
						string[] strs = g.Optarg.Split(',');
						npageses = new uint[strs.Length];

						for (int i = 0; i < strs.Length; i++)
						{
							if (!uint.TryParse(strs[i], out npageses[i]) || npageses[i] == 0)
								throw new InvalidCmdLineArgumentException("Positive integer(s) are expected after -p");
						}

						break;

					case 'r':
						if (!decimal.TryParse(g.Optarg, out readCost) || readCost <= 0)
							throw new InvalidCmdLineArgumentException("A positive integer is expected after -r");
						break;

					case 'w':
						if (!decimal.TryParse(g.Optarg, out writeCost) || writeCost <= 0)
							throw new InvalidCmdLineArgumentException("A positive integer is expected after -w");
						break;

					case ':':
						throw new InvalidCmdLineArgumentException("Uncomplete argument: -" + (char)g.Optopt);
					case '?':
						throw new InvalidCmdLineArgumentException("Invalid argument: -" + (char)g.Optopt);
					default:
						break;
				}
			}


			if (algos.Count == 0)
				algorithms = new AlgorithmSpec[] { new AlgorithmSpec("Trival") };
			else
				algorithms = algos.ToArray();

			if (args.Length > g.Optind)
				filename = args[g.Optind];
			else
				filename = null;
		}
	}
}