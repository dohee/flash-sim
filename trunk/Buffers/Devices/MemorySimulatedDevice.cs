using System;
using System.Collections.Generic;
using System.IO;

namespace Buffers.Devices
{
	public class MemorySimulatedDevice : BlockDeviceBase
	{
		private readonly MemoryStream stream = new MemoryStream();

		public MemorySimulatedDevice(uint pagesize)
		{
			PageSize = pagesize;
		}

		#region Derived Dispose 函数族
		private bool _disposed_MemorySimulatedDevice = false;

		protected override void Dispose(bool isDisposing)
		{
			if (_disposed_MemorySimulatedDevice)
				return;

			if (isDisposing)
				stream.Close(); // 清理托管资源

			// 清理非托管资源

			base.Dispose(isDisposing);
			_disposed_MemorySimulatedDevice = true;
		}
		#endregion

		public override string Name { get { return "MemoryDevice"; } }
		public override string Description { get { return Utils.FormatDescription("PageSize", PageSize); } }
		public MemoryStream Stream { get { return stream; } }

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
