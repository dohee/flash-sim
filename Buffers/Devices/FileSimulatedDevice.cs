using System;
using System.Collections.Generic;
using System.IO;
using Buffers.Utilities;

#if! __MonoCS__
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
#endif


namespace Buffers.Devices
{
	public class FileSimulatedDevice : BlockDeviceBase
	{
		private readonly FileStream stream;
		private readonly uint npages;
		private readonly bool canseek;

		public FileSimulatedDevice(uint pagesize, string path)
			: this(pagesize, path, false) { }
		public FileSimulatedDevice(uint pagesize, string path, bool checkbound)
			: this(pagesize, path, checkbound, false) { }
		public FileSimulatedDevice(uint pagesize, string path, bool checkbound, bool deleteBeforeOpen)
			: this(pagesize, path, checkbound, deleteBeforeOpen, 0) { }
		public FileSimulatedDevice(uint pagesize, string path, bool checkbound,
			bool deleteBeforeOpen, uint npages)
		{
			PageSize = pagesize;

			if (deleteBeforeOpen)
				File.Delete(path);

#if! __MonoCS__
			SafeFileHandle handle = UnmanagedFileIO.CreateFile(
				path,
				UnmanagedFileIO.GENERIC_READ | UnmanagedFileIO.GENERIC_WRITE,
				UnmanagedFileIO.FILE_SHARE_READ,
				IntPtr.Zero,
				UnmanagedFileIO.OPEN_ALWAYS,
				UnmanagedFileIO.FILE_FLAG_NO_BUFFERING | UnmanagedFileIO.FILE_FLAG_WRITE_THROUGH,
				IntPtr.Zero);

			if (handle.IsInvalid)
				Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());

			stream = new FileStream(handle, FileAccess.ReadWrite);
#else
			stream = new FileStream(path, FileMode.OpenOrCreate,
				FileAccess.ReadWrite, FileShare.Read, 1, FileOptions.WriteThrough);
#endif

			canseek = stream.CanSeek;

			if (stream.Length < npages * pagesize)
			{
				/*
				byte[] data = new byte[pagesize];
				new RandomDataGenerator().Generate(data);

				long pos = stream.Seek(0, SeekOrigin.End);
				long end = fixedPageCount * pagesize;

				if (pos < end)
				{
					long posCeil = ((pos + pagesize - 1) / pagesize) * pagesize;
					stream.Write(data, (int)(pos % pagesize), (int)(posCeil - pos));
					pos = posCeil;
				}

				for (; pos < end; pos += pagesize)
					stream.Write(data, 0, (int)pagesize);
				*/

				stream.SetLength(npages * pagesize);
			}

			if (checkbound)
				this.npages = (uint)(stream.Length / pagesize);
			else
				this.npages = 0xFFFFFFFF;
		}

		#region Derived Dispose 函数族
		private bool _disposed_FileSimulatedDevice = false;

		protected override void Dispose(bool isDisposing)
		{
			if (_disposed_FileSimulatedDevice)
				return;

			if (isDisposing)
				stream.Close(); // 清理托管资源

			// 清理非托管资源

			base.Dispose(isDisposing);
			_disposed_FileSimulatedDevice = true;
		}
		#endregion

		public override string Name { get { return "FileDevice"; } }
		public override string Description
		{
			get
			{
				return Utils.FormatDescription("PageSize", PageSize,
					"Path", stream.Name, "NPages", npages);
			}
		}
		public FileStream Stream { get { return stream; } }

		protected override void DoRead(uint pageid, byte[] result)
		{
			if (pageid >= npages)
				throw new ArgumentOutOfRangeException("pageid", "PageID larger than NPages");

			if (canseek)
				stream.Seek((long)pageid * PageSize, SeekOrigin.Begin);

			int i = stream.Read(result, 0, (int)PageSize);

			for (; i < result.Length; i++)
				result[i] = default(byte);
		}
		protected override void DoWrite(uint pageid, byte[] data)
		{
			if (pageid >= npages)
				throw new ArgumentOutOfRangeException("pageid", "PageID larger than NPages");

			if (canseek)
				stream.Seek((long)pageid * PageSize, SeekOrigin.Begin);

			stream.Write(data, 0, (int)PageSize);
		}
	}
}
