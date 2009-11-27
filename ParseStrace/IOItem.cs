using System;
using System.Collections.Generic;
using System.Text;

namespace ParseStrace
{
	struct IOItem
	{
		public readonly string Filename;
		public readonly bool IsWrite;
		public readonly long Position;
		public readonly long Length;

		public IOItem(string filename, bool isWrite, long pos, long length)
		{
			Filename = filename;
			IsWrite = isWrite;
			Position = pos;
			Length = length;
		}

		public override string ToString()
		{
			return string.Format("IOItem[{0} {1} at {2} on {3}]",
				IsWrite ? "write" : "read", Length, Position, Filename);
		}
	}

}
