using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;


namespace ParseStrace
{
	enum AccessType : byte
	{
		Read = 0,
		Write = 1,
	}

	enum AccessRoutine : byte
	{
		Read,
		Readv,
		Write,
		Writev,
		Pread,
		Pwrite,

		Mmap,
		Mremap,
		Msync,
		Munmap,
	}

	enum FDType : byte
	{
		Unknown,
		File,
		Terminal,
		Pipe,
		Socket,
	}


	static class EnumExtentions
	{
		private static readonly string[] routineStrings;

		static EnumExtentions()
		{
			FieldInfo[] fields = typeof(AccessRoutine).GetFields(BindingFlags.Public | BindingFlags.Static);
			routineStrings = new string[fields.Length];

			for (int i = 0; i < routineStrings.Length; i++)
				routineStrings[i] = fields[i].Name;
		}


		public static string AccessRoutineToString(this AccessRoutine routine)
		{
			return routineStrings[(int)routine];
		}

		public static string FDTypeToString(this FDType fdtype)
		{
			switch (fdtype)
			{
				case FDType.Unknown: return "Unkn";
				case FDType.File: return "File";
				case FDType.Terminal: return "Term";
				case FDType.Pipe: return "Pipe";
				case FDType.Socket: return "Sock";
				default: return null;
			}
		}
	}
}
