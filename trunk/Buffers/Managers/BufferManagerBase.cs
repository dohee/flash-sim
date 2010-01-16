using System;
using Buffers;
using Buffers.Devices;
using Buffers.Memory;

namespace Buffers.Managers
{
	public abstract class BufferManagerBase : BlockDeviceBase, IBufferManager, IDisposable
	{
		protected IBlockDevice dev;
		protected readonly Pool pool;

		protected virtual void DoFlush() { }
		protected virtual void OnPoolFull() { }

		protected virtual void DoCascadeFlush()
		{
			DoFlush();
			IBufferManager mgr = dev as IBufferManager;

			if (mgr != null)
				mgr.CascadeFlush();
		}


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


		public BufferManagerBase(IBlockDevice device, uint npages)
		{
			this.dev = (device == null ? new TrivalBlockDevice() : device);
			this.pool = (npages == 0 ? null : new Pool(npages, dev.PageSize, OnPoolFull));
		}

		#region Dispose 函数族
		private bool _disposed_BufferManagerBase = false;

		~BufferManagerBase()
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
			if (_disposed_BufferManagerBase)
				return;

			DoFlush(); // 清理非托管资源

			_disposed_BufferManagerBase = true;
		}
		#endregion

		public IBlockDevice AssociatedDevice { get { return dev; } }
		public override uint PageSize { get { return dev.PageSize; } }
		public int FlushCount { get; private set; }

		public void Flush()
		{
			DoFlush();
			FlushCount++;
		}
		public virtual void CascadeFlush()
		{
			DoCascadeFlush();
			FlushCount++;
		}
	}
}