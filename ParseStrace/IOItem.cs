using System;
using System.Collections.Generic;
using System.Text;

namespace ParseStrace
{
	enum FDType : byte
	{
		File,
		Terminal,
		Pipe,
	}

	class IOItem
	{
		public readonly int Pid;
		public readonly string Filename;
		public readonly short FDNum;
		public readonly FDType FDType;
		public readonly bool IsWrite;
		public readonly long Position;
		public readonly long Length;

		public IOItem(int pid, string filename, short fd,
			bool isWrite, long pos, long length)
			: this(pid, filename, fd, isWrite, pos, length, FDType.File) { }

		public IOItem(int pid, string filename, short fd,
			bool isWrite, long pos, long length, FDType type)
		{
			Pid = pid;
			Filename = filename;
			FDNum = fd;
			FDType = type;
			IsWrite = isWrite;
			Position = pos;
			Length = length;
		}

		public override string ToString()
		{
			return string.Format(
				"IOItem[{0} Len={1} at Pos={2} on {3} via FD={4}]",
				IsWrite ? "write" : "read", Length,
				Position, Filename, FDNum);
		}

		public string TypeString
		{
			get
			{
				switch (FDType)
				{
					case FDType.File: return "File";
					case FDType.Terminal: return "Term";
					case FDType.Pipe: return "Pipe";
					default: return null;
				}
			}
		}

	}
}
