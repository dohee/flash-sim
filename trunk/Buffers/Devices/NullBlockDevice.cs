using System;

namespace Buffers.Devices
{
	public sealed class NullBlockDevice : BlockDeviceBase
	{
		public override string Name { get { return "NullBlock"; } }
		protected override void DoRead(uint pageid, byte[] result) { }
		protected override void DoWrite(uint pageid, byte[] data) { }
	}
}