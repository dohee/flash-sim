using System;

namespace Buffers.Devices
{
	public sealed class NullEEPROM : EEPROMBase
	{
		public NullEEPROM(ushort blockSize)
			: base(blockSize) { }

		public override string Name { get { return "NullEEPROM"; } }
		protected override void DoRead(uint pageid, byte[] result) { }
		protected override void DoWrite(uint pageid, byte[] data) { }
		protected override void DoErase(uint blockid) { }
	}
}