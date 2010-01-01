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
		private uint readIRR = 0, writeIRR = 0;
		private uint readRecency = 0, writeRecency = 0;
		public uint lastReadRecency = 0, lastWriteRecency = 0;

		public IRRFrame(uint id) : base(id) { }
		public IRRFrame(uint id, int slotid) : base(id, slotid) { }

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
			double p1=0, p2=0;
			double weightedReadRecency = (double)readRecency * 1 / 1;
			double weightedWriteRecency = (double)writeRecency * 1 / 1;

			double aveReadIRR, aveWriteIRR;

			if (readIRR == 0)
				aveReadIRR = weightedReadRecency;
			else
				aveReadIRR = ((double)readIRR + weightedReadRecency) / 2;

			if (writeIRR == 0)
				aveWriteIRR = weightedWriteRecency;
			else
				aveWriteIRR = ((double)writeIRR + weightedWriteRecency) / 2;

			if (aveReadIRR != 0)        //0 means this page has not been read before.
				p1=(double)Config.ReadCost / aveReadIRR;

			if (aveWriteIRR != 0)
				p2= (double)Config.WriteCost / aveWriteIRR;

			return Math.Max(p1, p2);
		}

		public override string ToString()
		{
			return string.Format("IRRFrame{{Id={0},Dirty={1},RR={2},WR={3},RIRR={4},WIRR={5}}}",
				id, dirty, readRecency, writeRecency, readIRR, writeIRR);
		}

	}
}
