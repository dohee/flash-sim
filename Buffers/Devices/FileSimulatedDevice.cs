using System;
using System.Collections.Generic;
using System.IO;

namespace Buffers.Devices
{
	public class FileSimulatedDevice : BlockDeviceBase, IDisposable
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

			stream = new FileStream(path, FileMode.OpenOrCreate,
				FileAccess.ReadWrite, FileShare.Read, 1, FileOptions.WriteThrough);

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
				npages = (uint)(stream.Length / pagesize);
			else
				npages = 0xFFFFFFFF;
		}


		#region Dispose 函数族
		private bool _disposed_FileSimulatedDevice = false;

		~FileSimulatedDevice()
		{
			Dispose(false);
		}
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool isDisposing)
		{
			if (_disposed_FileSimulatedDevice)
				return;

			if (isDisposing)
				stream.Dispose(); // 清理托管资源

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
				throw new ArgumentOutOfRangeException("PageID larger than NPages");

			if (canseek)
				stream.Seek((long)pageid * PageSize, SeekOrigin.Begin);

			int i = stream.Read(result, 0, (int)PageSize);

			for (; i < result.Length; i++)
				result[i] = default(byte);
		}
		protected override void DoWrite(uint pageid, byte[] data)
		{
			if (pageid >= npages)
				throw new ArgumentOutOfRangeException("PageID larger than NPages");

			if (canseek)
				stream.Seek((long)pageid * PageSize, SeekOrigin.Begin);

			stream.Write(data, 0, (int)PageSize);
		}
	}
}
