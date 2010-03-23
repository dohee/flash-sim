using System;

namespace Buffers.Devices
{
	public sealed class NullEEPROM : EEPROMBase
	{
		public NullEEPROM(byte blocksizebit)
		{
			BlockSizeBit = blocksizebit;
		}

		public override string Name { get { return "NullEEPROM"; } }
		public override uint PageSize { get { return 0; } protected set { } }
		protected override void DoRead(uint pageid, byte[] result) { }
		protected override void DoWrite(uint pageid, byte[] data) { }
		protected override void DoErase(uint blockid) { }
	}
}