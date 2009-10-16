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
	}

//This is the frame for inter reference recency
    public class IRRFrame : Frame
    {
        private uint readIRR;
        private uint writeIRR;

        public IRRFrame(uint id)
            : this(id, -1) { }

        public IRRFrame(uint id, int slotid)
            :base(id, slotid)
		{
            readIRR = 0;
            writeIRR = 0;
		}
    }

}
