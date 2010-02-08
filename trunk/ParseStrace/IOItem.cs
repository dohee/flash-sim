using System;
using System.Linq;

namespace ParseStrace
{
	class IOItem
	{
		public readonly int Pid;
		public readonly string Filename;
		public readonly short FDNum;
		public readonly FDType FDType;
		public readonly AccessType Access;
		public readonly AccessRoutine Routine;
		public readonly long Position;
		public readonly long Length;

		public IOItem(int pid, string filename, short fd, AccessType access,
			AccessRoutine routine, long pos, long length)
			: this(pid, filename, fd, access, routine, pos, length, FDType.File) { }

		public IOItem(int pid, string filename, short fd, AccessType access,
			AccessRoutine routine, long pos, long length, FDType type)
		{
			Pid = pid;
			Filename = filename;
			FDNum = fd;
			FDType = type;
			Access = access;
			Routine = routine;
			Position = pos;
			Length = length;
		}

		public override string ToString()
		{
			return string.Format(
				"IOItem[{0} Len={1} at Pos={2} on {3} via FD={4} of Type={5}]",
				Routine.AccessRoutineToString(),
				Length, Position, Filename, FDNum,
				FDType.FDTypeToString());
		}
	}
}
