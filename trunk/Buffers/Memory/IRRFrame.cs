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
		public uint readIRR;
		public uint writeIRR;
		public uint readRecency;
		public uint writeRecency;

		public IRRFrame(uint id)
			: this(id, -1) { }

		public IRRFrame(uint id, int slotid)
			: base(id, slotid)
		{
			readIRR = 0;
			writeIRR = 0;
		}

		
		/// <summary>
		/// get the power of this page for evict selection
		/// </summary>
		/// <returns></returns>
		public double getPower()
		{
			double power = 0;
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
