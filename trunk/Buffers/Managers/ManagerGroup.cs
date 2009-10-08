using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Buffers.Managers
{
	public sealed class ManagerGroup : BufferManagerBase, IEnumerable<IBufferManager>
	{
		private List<IBufferManager> mgrs = new List<IBufferManager>();


		public int Count { get { return mgrs.Count; } }
		public IBufferManager this[int index] { get { return mgrs[index]; } }
		public IEnumerator<IBufferManager> GetEnumerator() { return mgrs.GetEnumerator(); }
		IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

		public void Add(IBufferManager mgr)
		{
			if (mgrs.Count == 0)
			{
				dev = mgr;
			}
			else if (mgrs.Count != 0 && dev.PageSize != mgr.PageSize)
			{
				throw new ArgumentException("PageSize not uniform", "mgr");
			}
			
			mgrs.Add(mgr);
		}


		protected override void DoRead(uint pageid, byte[] result)
		{
			foreach (var mgr in mgrs)
				mgr.Read(pageid, result);
		}
		protected override void DoWrite(uint pageid, byte[] data)
		{
			foreach (var mgr in mgrs)
				mgr.Write(pageid, data);
		}
		protected override void DoFlush()
		{
			foreach (var mgr in mgrs)
				mgr.Flush();
		}

	}
}
