using System;

namespace Buffers.Managers
{
	public sealed class TrivalManager : BufferManagerBase
	{
		public TrivalManager()
			: base() { }
		public TrivalManager(IBlockDevice dev)
			: base(dev) { }

		protected override void DoFlush() { }

		protected override void DoRead(uint pageid, byte[] result)
		{
			dev.Read(pageid, result);
		}

		protected override void DoWrite(uint pageid, byte[] data)
		{
			dev.Write(pageid, data);
		}

	}
}