using System;
using System.Collections.Generic;
using System.Diagnostics;
using Buffers;

namespace Buffers.Memory
{
	public class Frame : IFrame
	{
		private uint id;
		private int slotid;
		private bool dirty = false;

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


	}
}
