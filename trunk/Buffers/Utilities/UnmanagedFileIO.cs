using System;
using System.Collections.Generic;

#if! __MonoCS__
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
#endif


namespace Buffers.Utilities
{

#if! __MonoCS__
	static class UnmanagedFileIO
	{
		public const short FILE_ATTRIBUTE_NORMAL = 0x80;
		public const uint GENERIC_READ = 0x80000000;
		public const uint GENERIC_WRITE = 0x40000000;
		public const uint FILE_SHARE_READ = 0x00000001;
		public const uint FILE_SHARE_WRITE = 0x00000002;
		public const uint FILE_SHARE_DELETE = 0x00000004;
		public const uint CREATE_NEW = 1;
		public const uint CREATE_ALWAYS = 2;
		public const uint OPEN_EXISTING = 3;
		public const uint OPEN_ALWAYS = 4;
		public const uint FILE_FLAG_NO_BUFFERING = 0x20000000;
		public const uint FILE_FLAG_WRITE_THROUGH = 0x80000000;

		[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		public static extern SafeFileHandle CreateFile(
			string lpFileName,
			uint dwDesiredAccess,
			uint dwShareMode,
			IntPtr lpSecurityAttributes,
			uint dwCreationDisposition,
			uint dwFlagsAndAttributes,
			IntPtr hTemplateFile);
	}
#endif

}
