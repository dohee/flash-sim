using System;
using System.IO;

namespace ParseStrace
{
	class FormatterInfo
	{
		public string Filename;

		public FormatterInfo(string filename)
		{
			Filename = filename;
		}
	}

	abstract class IOItemFormatter
	{
		public abstract void PhaseBefore(FormatterInfo info);
		public abstract void PhaseOne(IOItem item);
		public abstract void PhaseBetween();
		public abstract void PhaseTwo(IOItem item);

		protected readonly TextWriter writer;

		public IOItemFormatter(TextWriter writer)
		{
			this.writer = writer;
		}
	}
}
