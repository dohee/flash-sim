using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Buffers.Memory
{
	public interface IFrame
	{
		uint Id { get; set; }
		bool Dirty { get; set; }
		bool Resident { get; }
		int DataSlotId { get; set; }
	}

	public class Frame : IFrame
	{
		private uint id;
		private int slotid;
		private bool dirty = false;

		public uint Id { get { return id; } set { id = value; } }
		public bool Dirty { get { return dirty; } set { dirty = value; } }
		public bool Resident { get { return slotid >= 0; } }
		public int DataSlotId { get { return slotid; } set { slotid = value; } }

		public Frame()
			: this(-1) { }

		public Frame(int slotid)
		{
			this.slotid = slotid;
		}


	}
}
