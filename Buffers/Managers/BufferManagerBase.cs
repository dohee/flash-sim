using System;
using Buffers;
using Buffers.Devices;
using Buffers.Memory;
using Buffers.Utilities;

namespace Buffers.Managers
{
	public abstract class BufferManagerBase : BlockDeviceBase, IBufferManager
	{
		// 字段
		protected IBlockDevice dev;
		protected readonly Pool pool;

		// 子类要实现的
		protected virtual void OnPoolFull() { }
		protected virtual void DoFlush() { }
		protected virtual void DoCascadeFlush()
		{
			DoFlush();
			IBufferManager mgr = dev as IBufferManager;

			if (mgr != null)
				mgr.CascadeFlush();
		}

		// 可供使用的
		protected void PerformAccess(IFrame frame, byte[] resultOrData, AccessType type)
		{
			if (!frame.Resident)
			{
				frame.DataSlotId = pool.AllocSlot();
				if (type == AccessType.Read)
					dev.Read(frame.Id, pool[frame.DataSlotId]);
			}

			if (type == AccessType.Read)
			{
				pool[frame.DataSlotId].CopyTo(resultOrData, 0);
			}
			else
			{
				resultOrData.CopyTo(pool[frame.DataSlotId], 0);
				frame.Dirty = true;
			}
		}
		protected void WriteIfDirty(IFrame frame)
		{
			if (frame.Dirty)
			{
				dev.Write(frame.Id, pool[frame.DataSlotId]);
				frame.Dirty = false;
			}
		}

		// 已实现的
		public BufferManagerBase(IBlockDevice device, uint npages)
		{
			this.dev = (device == null ? new NullBlockDevice() : device);
			this.pool = (npages == 0 ? null : new Pool(npages, dev.PageSize, OnPoolFull));
			this.PageSize = dev.PageSize;
		}

		#region Derived Dispose 函数族
		private bool _disposed_BufferManagerBase = false;

		protected override void Dispose(bool isDisposing)
		{
			if (_disposed_BufferManagerBase)
				return;

			if (isDisposing) // 清理托管资源
			{
				DoFlush();
				dev.Dispose();
			}

			// 清理非托管资源

			base.Dispose(isDisposing);
			_disposed_BufferManagerBase = true;
		}
		#endregion

		public IBlockDevice BaseDevice { get { return dev; } }
		public int FlushCount { get; private set; }

		public void Flush()
		{
			DoFlush();
			FlushCount++;
		}
		public void CascadeFlush()
		{
			DoCascadeFlush();
			FlushCount++;
		}
	}
}