using System;
using System.Linq;

namespace ParseStrace
{
	[Flags]
	enum AccessType : byte
	{
		None = 0,
		Read = 1,
		Write = 2,
		FileRoutine = 4,
		MmapRoutine = 8,
	}

	enum FDType : byte
	{
		Unknown,
		File,
		Terminal,
		Pipe,
		Socket,
	}

	class IOItem
	{
		public readonly int Pid;
		public readonly string Filename;
		public readonly short FDNum;
		public readonly FDType FDType;
		public readonly AccessType Access;
		public readonly long Position;
		public readonly long Length;

		public IOItem(int pid, string filename, short fd,
			AccessType access, long pos, long length)
			: this(pid, filename, fd, access, pos, length, FDType.File) { }

		public IOItem(int pid, string filename, short fd,
			AccessType access, long pos, long length, FDType type)
		{
			Pid = pid;
			Filename = filename;
			FDNum = fd;
			FDType = type;
			Access = access;
			Position = pos;
			Length = length;
		}

		public override string ToString()
		{
			return string.Format(
				"IOItem[{0} Len={1} at Pos={2} on {3} via FD={4} of Type={5}]",
				Access.AccessTypeToString(),
				Length, Position, Filename, FDNum,
				FDType.FDTypeToString());
		}
	}
}
