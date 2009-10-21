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
			double weightedReadRecency = (double)readRecency * 1 / 4;
			double weightedWriteRecency = (double)writeRecency * 1 / 4;

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
				power += (double)Config.ReadCost / aveReadIRR;

			if (aveWriteIRR != 0)
				power += (double)Config.WriteCost / aveWriteIRR;

			return power;
		}

		public override string ToString()
		{
			return string.Format("IRRFrame{{Id={0},Dirty={1},RR={2},WR={3},RIRR={4},WIRR={5}}}",
				id, dirty, readRecency, writeRecency, readIRR, writeIRR);
		}

	}
}
