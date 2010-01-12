using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Buffers.Managers
{
	public sealed class ManagerGroup : BufferManagerBase, IEnumerable<IBufferManager>
	{
		private List<IBufferManager> mgrs = new List<IBufferManager>();

		public ManagerGroup()
			: base(null, 0) { }

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


		protected override void DoAccess(uint pageid, byte[] resultOrData, AccessType type)
		{
			if (type == AccessType.Read)
				foreach (var mgr in mgrs)
					mgr.Read(pageid, resultOrData);
			else
				foreach (var mgr in mgrs)
					mgr.Write(pageid, resultOrData);
		}
		protected override void DoFlush()
		{
			foreach (var mgr in mgrs)
				mgr.Flush();
		}

	}
}
