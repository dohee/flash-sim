using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Buffers.Managers
{
	public sealed class ManagerGroup : BufferManagerBase, IEnumerable<IBufferManager>
	{
		private List<IBufferManager> mgrs = new List<IBufferManager>();
		private bool verifyRead;

		public ManagerGroup()
			: this(false) { }
		public ManagerGroup(bool verifyRead)
			: base(null, 0) { this.verifyRead = verifyRead; }

		#region Derived Dispose 函数族
		private bool _disposed_ManagerGroup = false;

		protected override void Dispose(bool isDisposing)
		{
			if (_disposed_ManagerGroup)
				return;

			if (isDisposing) // 清理托管资源
			{
				DoFlush();
				foreach (var mgr in mgrs)
					mgr.Dispose();
			}

			// 清理非托管资源

			base.Dispose(isDisposing);
			_disposed_ManagerGroup = true;
		}
		#endregion

		public int Count { get { return mgrs.Count; } }
		public IBufferManager this[int index] { get { return mgrs[index]; } }
		public IEnumerator<IBufferManager> GetEnumerator() { return mgrs.GetEnumerator(); }
		IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

		public void Add(IBufferManager mgr)
		{
			if (mgrs.Count == 0)
				dev = mgr;
			else if (dev.PageSize != mgr.PageSize)
				throw new ArgumentException("PageSize not uniform", "mgr");

			mgrs.Add(mgr);
		}

		protected override void DoRead(uint pageid, byte[] result)
		{
			if (verifyRead && mgrs.Count > 1)
			{
				mgrs[0].Read(pageid, result);

				for (int i = 1; i < mgrs.Count; i++)
				{
					byte[] other = new byte[PageSize];					
					mgrs[i].Read(pageid, other);

					if (!Utils.ArrayEqual(result, other))
						throw new DataNotConsistentException(string.Format(
							"Read result not consistent at Page {0} between Device 0 and Device {1}",
							pageid, i));
				}
			}
			else
			{
				foreach (var mgr in mgrs)
					mgr.Read(pageid, result);
			}
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
		protected override void DoCascadeFlush()
		{
			foreach (var mgr in mgrs)
				mgr.CascadeFlush();
		}
	}

}
