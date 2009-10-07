using System;

namespace Buffers
{
	public interface IBufferManager : IBlockDevice
	{
		IBlockDevice AssociatedDevice { get; }
		void Flush();
	}
}

namespace Buffers.Managers
{
	public abstract class BufferManagerBase : Buffers.IBufferManager, IDisposable
	{
		protected IBlockDevice dev;
		private int read = 0, write = 0;
		private bool disposed = false;

		protected abstract void DoRead(uint pageid, byte[] result);
		protected abstract void DoWrite(uint pageid, byte[] data);
		protected abstract void DoFlush();

		public BufferManagerBase(IBlockDevice device)
		{
			this.dev = device;
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

		public IBlockDevice AssociatedDevice { get { return dev; } }
		public uint PageSize { get { return dev.PageSize; } }
		public int ReadCount { get { return read; } }
		public int WriteCount { get { return write; } }

		public void Read(uint pageid, byte[] result)
		{
			DoRead(pageid, result);
			read++;
		}
		public void Write(uint pageid, byte[] data)
		{
			DoWrite(pageid, data);
			write++;
		}
		public void Flush()
		{
			DoFlush();
		}
	}


	public sealed class TrivalManager : BufferManagerBase
	{
		public TrivalManager(IBlockDevice dev)
			: base(dev) { }

		protected override void DoRead(uint pageid, byte[] result)
		{
			dev.Read(pageid, result);
		}

		protected override void DoWrite(uint pageid, byte[] data)
		{
			dev.Write(pageid, data);
		}

		protected override void DoFlush() { }
	}
}