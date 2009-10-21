using System;
using System.Collections.Generic;
using System.Diagnostics;
using Buffers;

namespace Buffers.Memory
{
	public class Frame : IFrame
	{
		protected uint id;
		protected int slotid;
		protected bool dirty = false;

		public uint Id { get { return id; } set { id = value; } }
		public bool Dirty { get { return dirty; } set { dirty = value; } }
		public bool Resident { get { return slotid >= 0; } }
		public int DataSlotId { get { return slotid; } set { slotid = value; } }

		public Frame(uint id)
			: this(id, -1) { }

		public Frame(uint id, int slotid)
		{
			this.id = id;
			this.slotid = slotid;
		}

		public override string ToString()
		{
			return string.Format("IRRFrame{{Id={0},Dirty={1},SlotId={2}}}",
				id, dirty, slotid);
		}
	}
}
