using System;
using System.IO;

namespace ParseStrace
{
	sealed class IOItemDirectlyToWriter : IOItemFormatter
	{
		public IOItemDirectlyToWriter(TextWriter writer)
			: base(writer) { }

		public override void PhaseBefore(FormatterInfo info) { }
		public override void PhaseOne(IOItem item) { }
		public override void PhaseBetween() { }
		public override void PhaseAfter() { }

		public override void PhaseTwo(IOItem item)
		{
			Output(writer, item);
		}

		public static void Output(TextWriter writer, IOItem item)
		{
			string typestring;

			if (item.FDType == FDType.File)
				typestring = "";
			else
				typestring = "<" + item.FDType.FDTypeToString() + ">";

			writer.WriteLine("{6,-10} {5,-8} at {4,-9} of Pid={1} FD={2}{3}: {0}",
				item.Filename, item.Pid, item.FDNum, typestring,
				item.Position, item.Length,
				item.Access.AccessTypeToString());
		}
	}

}