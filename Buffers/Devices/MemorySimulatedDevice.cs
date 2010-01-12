using System;
using System.Collections.Generic;
using System.IO;

namespace Buffers.Devices
{
	public class MemorySimulatedDevice : BlockDeviceBase, IDisposable
	{
		private readonly MemoryStream stream = new MemoryStream();

		public MemorySimulatedDevice(uint pagesize)
		{
			PageSize = pagesize;
		}

		#region Dispose 函数族
		private bool _disposed_MemorySimulatedDevice = false;

		~MemorySimulatedDevice()
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
			if (_disposed_MemorySimulatedDevice)
				return;

			if (isDisposing)
				stream.Dispose(); // 清理托管资源

			_disposed_MemorySimulatedDevice = true;
		}
		#endregion

		public byte[] ToArray()
		{
			return stream.ToArray();
		}

		protected override void DoRead(uint pageid, byte[] result)
		{
			stream.Seek((long)pageid * PageSize, SeekOrigin.Begin);
			int i = stream.Read(result, 0, (int)PageSize);

			for (; i < result.Length; i++)
				result[i] = default(byte);
		}
		protected override void DoWrite(uint pageid, byte[] data)
		{
			stream.Seek((long)pageid * PageSize, SeekOrigin.Begin);
			stream.Write(data, 0, (int)PageSize);
		}
	}
}
