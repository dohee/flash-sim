using System;

namespace Buffers.Managers
{
	public sealed class TrivalManager : BufferManagerBase
	{
		public TrivalManager()
			: this(null) { }
		public TrivalManager(IBlockDevice dev)
			: base(dev, 0) { }

		protected override void DoFlush() { }
		protected override void OnPoolFull() { }

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