using System;

namespace Buffers.Managers
{
	public sealed class TrivalManager : BufferManagerBase
	{
		public TrivalManager()
			: this(null) { }
		public TrivalManager(IBlockDevice dev)
			: base(dev) { }

		protected override void DoFlush() { }
		protected override void OnPoolFull() { }

		protected override void DoAccess(uint pageid, byte[] resultOrData, AccessType type)
		{
			if (type == AccessType.Read)
				dev.Read(pageid, resultOrData);
			else
				dev.Write(pageid, resultOrData);
		}
	}
}