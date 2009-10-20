using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Buffers.Memory
{
	/// <summary>
	/// This is the frame for inter reference recency
	/// </summary>
	public class IRRFrame : Frame
	{
		private uint readIRR, writeIRR, readRecency, writeRecency;

		public IRRFrame(uint id)
			: this(id, -1) { }

		public IRRFrame(uint id, int slotid)
			: base(id, slotid)
		{
			readIRR = 0;
			writeIRR = 0;
		}

		public uint ReadIRR { get { return readIRR; } set { readIRR = value; } }
		public uint WriteIRR { get { return writeIRR; } set { writeIRR = value; } }
		public uint ReadRecency { get { return readRecency; } set { readRecency = value; } }
		public uint WriteRecency { get { return writeRecency; } set { writeRecency = value; } }

		
		/// <summary>
		/// get the power of this page for evict selection
		/// </summary>
		/// <returns></returns>
		public double GetPower()
		{
			double power = 0;
            //readRecency = readRecency * 1 / 2;
            //writeRecency = writeRecency * 1 / 2;

			double aveReadIRR = ((double)readIRR + readRecency) / 2;
			double aveWriteIRR = ((double)writeIRR + writeRecency) / 2;

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
