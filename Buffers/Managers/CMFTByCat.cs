using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Buffers.Managers
{
	public class CMFTByCat:BufferManagerBase
	{
		public CMFTByCat():base(null)
		{

		}

		protected override void DoRead(uint pageid, byte[] result)
		{
			throw new NotImplementedException();
		}

		protected override void DoWrite(uint pageid, byte[] data)
		{
			throw new NotImplementedException();
		}

		protected override void DoFlush()
		{
			throw new NotImplementedException();
		}
	}
}
