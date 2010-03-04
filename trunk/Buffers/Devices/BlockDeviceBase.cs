using System;
using Buffers;

namespace Buffers.Devices
{
	public abstract class BlockDeviceBase : IBlockDevice
	{
		protected virtual void DoRead(uint pageid, byte[] result)
		{
			DoAccess(pageid, result, AccessType.Read);
		}
		protected virtual void DoWrite(uint pageid, byte[] data)
		{
			DoAccess(pageid, data, AccessType.Write);
		}
		protected virtual void DoAccess(uint pageid, byte[] resultOrData, AccessType type) { }


		public virtual string Name { get { return this.GetType().Name; } }
		public virtual string Description { get { return null; } }
		public virtual uint PageSize { get; protected set; }
		public int ReadCount { get; private set; }
		public int WriteCount { get; private set; }

		public void Read(uint pageid, byte[] result)
		{
			DoRead(pageid, result);
			ReadCount++;
		}
		public void Write(uint pageid, byte[] data)
		{
			DoWrite(pageid, data);
			WriteCount++;
		}

		#region Dispose 函数族
		private bool _disposed_BlockDeviceBase = false;

		~BlockDeviceBase()
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
			if (_disposed_BlockDeviceBase)
				return;

			if (isDisposing) { }// 清理托管资源

			// 清理非托管资源

			_disposed_BlockDeviceBase = true;
		}
		#endregion
	}
}