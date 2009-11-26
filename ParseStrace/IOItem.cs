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
	}

}
