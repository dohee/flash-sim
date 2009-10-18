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
        public uint readIRR;
        public uint writeIRR;
        public uint readRecency;
        public uint writeRecency;

        public IRRFrame(uint id)
            : this(id, -1) { }

        public IRRFrame(uint id, int slotid)
            :base(id, slotid)
		{
            readIRR = 0;
            writeIRR = 0;
		}

        //get the power of this page for evict selection
        public double getPower()
        {
            double power = 0;
            double aveReadIRR = ((double)readIRR+readRecency)/2;
            double aveWriteIRR = ((double)writeIRR+writeRecency)/2;

            if (readIRR == 0) aveReadIRR = readRecency; //only read once
            if (writeIRR == 0) aveWriteIRR = writeRecency;  //only write once

            if (aveReadIRR != 0)        //0 means this page has not been read before.
            {
                power += (double)Config.ReadCost / aveReadIRR;
            }
            if (aveWriteIRR != 0)
            {
                power += (double)Config.WriteCost / aveWriteIRR;
            }
            return power;
        }

    }

}
