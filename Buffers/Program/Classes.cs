using System;
using System.Collections.Generic;

namespace Buffers.Program
{
	struct AlgorithmSpec
	{
		public readonly string Name;
		public readonly string[] Arguments;

		public AlgorithmSpec(string name)
			: this(name, new string[0]) { }

		public AlgorithmSpec(string name, string[] arguments)
		{
			Name = name;
			Arguments = arguments;
		}

		public string ArgumentString
		{
			get
			{
				if (Arguments.Length == 0)
					return "";
				else
					return "(" + string.Join(",", Arguments) + ")";
			}
		}

		public override string ToString()
		{
			return Name + ArgumentString;
		}
	}


	class DevStatInfo
	{
		public string Id, Name, Description;
		public int Read, Write, Flush;
		public decimal Cost;
		public bool Suppress;
	}


	enum RunMode
	{
		Normal,
		Verify,
		File,
		Trace,
	}

	struct RunModeInfo
	{
		public readonly RunMode Mode;
		public readonly object ExtInfo;

		public RunModeInfo(RunMode mode, object extinfo)
		{
			Mode = mode;
			ExtInfo = extinfo;
		}
	}
}
