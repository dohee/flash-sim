using System;
using System.Collections.Generic;
using System.Text;

namespace ParseStrace
{
	struct IOItem
	{
		public readonly string Filename;
		public readonly short FD;
		public readonly bool IsTerminal;
		public readonly bool IsWrite;
		public readonly long Position;
		public readonly long Length;

		public IOItem(string filename, short fd, bool isWrite, long pos, long length)
			: this(filename, fd, isWrite, pos, length, false) { }

		public IOItem(string filename, short fd, bool isWrite, long pos, long length, bool isTerminal)
		{
			Filename = filename;
			FD = fd;
			IsTerminal = isTerminal;
			IsWrite = isWrite;
			Position = pos;
			Length = length;
		}

		public override string ToString()
		{
			return string.Format(
				"IOItem[{0} Len={1} at Pos={2} on {3} via FD={4}]",
				IsWrite ? "write" : "read", Length,
				Position, Filename, FD);
		}
	}

}
