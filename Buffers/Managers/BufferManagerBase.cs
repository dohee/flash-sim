using System;
using Buffers;
using Buffers.Devices;

namespace Buffers.Managers
{
	public abstract class BufferManagerBase : Buffers.IBufferManager, IDisposable
	{
		protected IBlockDevice dev;
		private int read = 0, write = 0, flush = 0;
		private bool disposed = false;

		protected virtual void DoAccess(uint pageid, byte[] resultOrData, AccessType type)
		{
			if (type == AccessType.Read)
				DoRead(pageid, resultOrData);
			else
				DoWrite(pageid, resultOrData);
		}
		protected abstract void DoFlush();

		protected virtual void DoRead(uint pageid, byte[] result) { }
		protected virtual void DoWrite(uint pageid, byte[] data) { }

		public BufferManagerBase(IBlockDevice device)
		{
			this.dev = (device == null ? new TrivalBlockDevice() : device);
		}
		~BufferManagerBase()
		{
			Dispose(false);
		}
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		private void Dispose(bool disposing)
		{
			if (disposed)
				return;

			DoFlush();
			disposed = true;
		}

		public virtual string Name { get { return this.GetType().Name; } }
		public virtual string Description { get { return null; } }
		public IBlockDevice AssociatedDevice { get { return dev; } }
		public uint PageSize { get { return dev.PageSize; } }
		public int ReadCount { get { return read; } }
		public int WriteCount { get { return write; } }
		public int FlushCount { get { return flush; } }

		public void Read(uint pageid, byte[] result)
		{
			DoAccess(pageid, result, AccessType.Read);
			read++;
		}
		public void Write(uint pageid, byte[] data)
		{
			DoAccess(pageid, data, AccessType.Write);
			write++;
		}
		public void Flush()
		{
			DoFlush();
			flush++;
		}
	}
}