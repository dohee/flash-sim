using System;
using System.Collections.Generic;

namespace Buffers.Program
{
	class DevStatInfo
	{
		public string Id, Name, Description;
		public int Read, Write, Flush;
		public decimal Cost;
		public bool Suppress;
	}

	struct AlgorithmSpec
	{
		public string Name;
		public string[] Arguments;

		public AlgorithmSpec(string name)
			: this(name, new string[0]) { }

		public AlgorithmSpec(string name, string[] arguments)
		{
			Name = name;
			Arguments = arguments;
		}
	}
}
